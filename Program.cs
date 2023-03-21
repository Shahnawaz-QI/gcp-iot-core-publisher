using System.Text;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

using M2Mqtt;
using M2Mqtt.Messages;

using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Crypto.Parameters;

const string mqttBrokerHostName = "mqtt.googleapis.com";
const int mqttBrokerPort = 8883;

const string projectId = "<YOUR_GCP_PROJECT_ID>";
const string region = "<GCP_REGION>";
const string registryId = "<IOT_CORE_REGISTRY_ID>";
const string deviceId = "<DEVICE_ID>";

const string eventsTopic = $"/devices/{deviceId}/events";
const string mqttClientId = $"projects/{projectId}/locations/{region}/registries/{registryId}/devices/{deviceId}";

const string pathToGoogleRootCertificateFile = @"C:\Documents\gcp-iot-core-publisher\gtsltsr.crt";
const string pathToPrivateKeyForDevice = @"C:\Documents\gcp-iot-core-publisher\rsa_private.pem";

try
{
    // Load Google Root Certificate
    X509Certificate x509_roots = new(fileName: pathToGoogleRootCertificateFile);

    // Create JWT token from private key file of the device registered into GCP
    string token = CreateToken(pathToPrivateKeyForDevice: pathToPrivateKeyForDevice);

    // Create MQTT client
    MqttClient mqttClient = new(brokerHostName: mqttBrokerHostName,
                                brokerPort: mqttBrokerPort,
                                secure: true,
                                caCert: x509_roots,
                                clientCert: null,
                                sslProtocol: MqttSslProtocols.TLSv1_2)      
    {
        ProtocolVersion = MqttProtocolVersion.Version_3_1_1
    };

    // Register event handlers for debugging
    mqttClient.ConnectionClosed += MqttClient_ConnectionClosed;
    mqttClient.MqttMsgPublished += MqttClient_MqttMsgPublished;

    mqttClient.Connect(clientId: mqttClientId, username: null, password: token, cleanSession: false, keepAlivePeriod: 10);
    if (mqttClient.IsConnected)
    {
        foreach (int number in Enumerable.Range(start: 1, count: 10))
        {
            string payload = $"Data packet #{number} to GCP IoT Core.";
            byte[] message = Encoding.Unicode.GetBytes(s: payload);

            // Publish to GCP IoT Core topic with QoS: 0 (No acknowledgement required)
            ushort returnValue = mqttClient.Publish(topic: eventsTopic,
                                                    message: message,
                                                    qosLevel: MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE,
                                                    retain: true);

            if (returnValue > 0)
            {
                Console.WriteLine("Publish successful.");
            }
            Thread.Sleep(TimeSpan.FromSeconds(1));
        }
        mqttClient.Disconnect();
    }
    else
    {
        Console.WriteLine("Unable to connect to GCP IoT Core.");
    }
}
catch (Exception ex)
{
    Console.WriteLine(ex.Message);
}

static void MqttClient_MqttMsgPublished(object sender, MqttMsgPublishedEventArgs e)
{
    Console.WriteLine("Message successfully published to GCP IoT Core.");
}

static void MqttClient_ConnectionClosed(object sender, EventArgs e)
{
    Console.WriteLine("Connection to GCP IoT Core closed.");
}

static string CreateToken(string pathToPrivateKeyForDevice)
{
    // Reads private key file content, create encrypted JWT token using RS256 algorithm
    string privateKeyFileContent = File.ReadAllText(path: pathToPrivateKeyForDevice);

    long iat = DateTimeOffset.Now.ToUnixTimeSeconds();
    long exp = iat + 3600;          // Token expires in 3600 seconds / 1 Hour.

    Dictionary<string, object> claims = new()
    {
        {"iat", iat},               // Issued at
        {"exp", exp},               // Expiration time
        {"aud", projectId}          // Audience is GCP project
    };

    RSAParameters rsaParams;
    using (StringReader stringReader = new(privateKeyFileContent))
    {
        PemReader pemReader = new(stringReader);
        if (pemReader.ReadObject() is not RsaPrivateCrtKeyParameters rsaPrivateKey)
            throw new InvalidDataException("Could not read RSA private key.");

        rsaParams = DotNetUtilities.ToRSAParameters(rsaPrivateKey);
    }
    using RSACryptoServiceProvider rsaCryptoServiceProvider = new(dwKeySize: 2048);
    rsaCryptoServiceProvider.ImportParameters(rsaParams);
    Dictionary<string, object> payload = claims.ToDictionary(k => k.Key, v => v.Value);
    return Jose.JWT.Encode(payload, rsaCryptoServiceProvider, Jose.JwsAlgorithm.RS256);
}