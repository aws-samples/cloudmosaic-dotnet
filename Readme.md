# CloudMosaic 2019 Edition

This is the source drop for the version of CloudMosaic used for breakout session "WIN308 - Developing serverless .NET Core on AWS" at AWS re:Invent 2019. The re:Invent session will be recorded and uploaded to Youtube. Once the session is uploaded a link will be added here.

The 2019 version has the following changes over the version in the master branch.

* Example of doing aggregration in Amazon DynamoDB using AWS Lambda and DynamoDB Streams.
* AWS Step Function state machine enhanced to have error handling with catch clauses in state-machine.json.
* New ASP.NET Core 3.1 Web API project using JWT tokens vended by Cognito to handle authenication. The project is hosted in AWS Lambda using Lambda custom runtime support and the AWS .NET tooling that simplies using .NET Core 3.1 with Lambda custom runtimes.
* New frontend using ASP.NET Core new service-side Blazor web framework hosted in ECS using AWS Fargate.
* Use API Gateway's WebSocket support to connect backend components to the frontend allowing backend systems to easily communicate their status to the user in real time.


More content will be added to this readme to explain the code in this repository.