# GCP IoT Core Publisher

This app publishes sample data to GCP IoT Core topic over MQTT protocol. The sample codes listed in GCP documentation page lacks `.NET / C#` example.

### Pre-requisites

- `.NET 7` SDK.
- Visual Studio 2022 v 17.5.2+ / VS Code with OmniSharp extension / JetBrains RIDER.
- Google Cloud Platform account with IoT Core enabled.
- GCP related [requrements](https://cloud.google.com/iot/docs/requirements).
- [OpenSSL](https://github.com/openssl/openssl) tool to generate `RSA` **public-private key** pair for device authentication in GCP.

### Setup in Google Cloud Platform:

- Activate IoT Core service in GCP.
- Create a device registry with `MQTT` protocol support.
- Create a device
	- Set `Device communication` to `Allow`.
	- Set `Authentication` > `Public key format` to `RS256`.
	- Use the following command in `Terminal / PowerShell` to generate RSA key pairs:
	```
	openssl genpkey -algorithm RSA -out rsa_private.pem -pkeyopt rsa_keygen_bits:2048
	openssl rsa -in rsa_private.pem -pubout -out rsa_public.pem
	```
	- Use the `public` key file content and upload it as `public key value` in device registration UI. The `private` key file will be used in programmatic access.
	
- Link the device with the device registry
- Follow the other instructions described [here](https://cloud.google.com/iot/docs/how-tos/mqtt-bridge).

#### Disclaimer:

_GCP IoT Core will be shutdown on August 16, 2023_ ([Link](https://cloud.google.com/iot/docs/release-notes#August_16_2022)).


