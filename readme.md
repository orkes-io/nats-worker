# Simple NATS Worker
Simple NATS worker that listens on a stream (optionally filtered by a specific subject), pauses for an artificial delay, then sends the incoming payload back to a response subject.

# Configuration
1. Edit `config.json` with the location of your NATS server and any required credentials
2. Edit `Program.cs`:
  - `requestsStream` - stream name where the request subject lives
  - `requestsTopicPrefix` - request topic prefix e.g. `requests.`
  - `responseTopic` - subject name to send responses to

# Launch
`dotnet run`
Launches a worker that listens on all subjects prefixed by `requestsTopicPrefix`

`dotnet run <subject>`
Launches a worker that filters on the specific subject prefixed by `requestsTopicPrefix` e.g. `dotnet run abc` will listen on `requests.abc`
