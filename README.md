# auth-grpc

cd auth-grpc
dotnet new sln -n auth-grpc

cd auth-grpc
dotnet new sln -n auth-grpc

# Shared libs
dotnet new classlib -n Common.Contracts -o src/Common/Common.Contracts
dotnet new classlib -n Common.Messaging -o src/Common/Common.Messaging
dotnet new classlib -n Common.Security -o src/Common/Common.Security
dotnet new classlib -n Common.Observability -o src/Common/Common.Observability
dotnet new classlib -n Common.Locks -o src/Common/Common.Locks

# Auth service
dotnet new webapi -n Auth.API -o src/Services/Auth/Auth.API --no-https
dotnet new grpc   -n Auth.Grpc -o src/Services/Auth/Auth.Grpc
dotnet new classlib -n Auth.Core -o src/Services/Auth/Auth.Core
dotnet new classlib -n Auth.Infrastructure -o src/Services/Auth/Auth.Infrastructure

# Customer service
dotnet new webapi -n Customer.API -o src/Services/Customer/Customer.API --no-https
dotnet new grpc   -n Customer.Grpc -o src/Services/Customer/Customer.Grpc
dotnet new classlib -n Customer.Core -o src/Services/Customer/Customer.Core
dotnet new classlib -n Customer.Infrastructure -o src/Services/Customer/Customer.Infrastructure

# Add all to solution
dotnet sln add src/**/**/*.csproj


/// add packages
dotnet add src/Services/Auth/Auth.API package Microsoft.AspNetCore.Authentication.JwtBearer
dotnet add src/Services/Auth/Auth.API package MassTransit.RabbitMQ
dotnet add src/Services/Auth/Auth.API package Serilog.AspNetCore
dotnet add src/Services/Auth/Auth.API package OpenTelemetry.Exporter.OpenTelemetryProtocol
dotnet add src/Services/Auth/Auth.API package OpenTelemetry.Extensions.Hosting
dotnet add src/Services/Auth/Auth.API package OpenTelemetry.Instrumentation.AspNetCore

dotnet add src/Services/Customer/Customer.API package Microsoft.AspNetCore.Authentication.JwtBearer
dotnet add src/Services/Customer/Customer.API package MassTransit 
dotnet add src/Services/Customer/Customer.API package MassTransit.RabbitMQ
dotnet add src/Services/Customer/Customer.API package Serilog.AspNetCore
dotnet add src/Services/Customer/Customer.API package OpenTelemetry.Exporter.OpenTelemetryProtocol
dotnet add src/Services/Customer/Customer.API package OpenTelemetry.Extensions.Hosting
dotnet add src/Services/Customer/Customer.API package OpenTelemetry.Instrumentation.AspNetCore

dotnet add src/Services/Auth/Auth.Infrastructure package Npgsql.EntityFrameworkCore.PostgreSQL 
dotnet add src/Services/Auth/Auth.Infrastructure package Microsoft.EntityFrameworkCore.Design
dotnet add src/Services/Customer/Customer.Infrastructure package Npgsql.EntityFrameworkCore.PostgreSQL 
dotnet add src/Services/Customer/Customer.Infrastructure package Microsoft.EntityFrameworkCore.Design

dotnet add src/Services/Auth/Auth.Grpc package Grpc.AspNetCore 
dotnet add src/Services/Auth/Auth.Grpc package Grpc.Tools
dotnet add src/Services/Customer/Customer.Grpc package Grpc.AspNetCore 
dotnet add src/Services/Customer/Customer.Grpc package Grpc.Tools

dotnet add src/Common/Common.Messaging package MassTransit 
dotnet add src/Common/Common.Messaging package MassTransit.RabbitMQ
dotnet add src/Common/Common.Security package Microsoft.IdentityModel.Tokens 
dotnet add src/Common/Common.Security package System.IdentityModel.Tokens.Jwt
dotnet add src/Common/Common.Observability package Serilog.AspNetCore
dotnet add src/Common/Common.Locks package StackExchange.Redis 
dotnet add src/Common/Common.Locks package RedLock.net

# to start the infrastructure
docker compose up -d
# in separate terminals
# Auth.API
ASPNETCORE_URLS=http://localhost:5000 dotnet run --project src/Services/Auth/Auth.API --no-launch-profile
# Auth.Grpc
ASPNETCORE_URLS=http://localhost:5001 dotnet run --project src/Services/Auth/Auth.Grpc --no-launch-profile
# Customer.API
ASPNETCORE_URLS=http://localhost:5002 dotnet run --project src/Services/Customer/Customer.API --no-launch-profile
# Customer.Grpc
ASPNETCORE_URLS=http://localhost:5003 dotnet run --project src/Services/Customer/Customer.Grpc --no-launch-profile


additional tools
brew install grpcurl

add reflection for auth grpc
dotnet add src/Services/Auth/Auth.Grpc package Grpc.AspNetCore.Server.Reflection
