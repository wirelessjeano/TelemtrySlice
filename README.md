```
                                                                                                  
,--------.      ,--.                          ,--.                   ,---.  ,--.,--.              
'--.  .--',---. |  | ,---. ,--,--,--. ,---. ,-'  '-.,--.--.,--. ,--.'   .-' |  |`--' ,---. ,---.  
   |  |  | .-. :|  || .-. :|        || .-. :'-.  .-'|  .--' \  '  / `.  `-. |  |,--.| .--'| .-. : 
   |  |  \   --.|  |\   --.|  |  |  |\   --.  |  |  |  |     \   '  .-'    ||  ||  |\ `--.\   --. 
   `--'   `----'`--' `----'`--`--`--' `----'  `--'  `--'   .-'  /   `-----' `--'`--' `---' `----' 
                                                           `---'                                  
```

# TelemetrySlice

## About the Project

### Built With

#### Frontend:

- [Next.js](https://nextjs.org)
- [tRPC](https://trpc.io)
- [React.js](https://reactjs.org)
- [Tailwind CSS](https://tailwindcss.com)
- [Yarn](https://yarnpkg.com/)
- [Turbo](https://turborepo.dev/)
- [Recharts](https://recharts.github.io/)
- [Tanstack Query](https://tanstack.com/query/latest)
- [daisyUI](https://daisyui.com/)
- [TypeScript](https://www.typescriptlang.org/)
  
#### Backend:

- [.NET / C#](https://dotnet.microsoft.com/en-us/)
- [Entity Framework Core](https://learn.microsoft.com/en-us/ef/core/)
- [StackExchange.Redis](https://github.com/StackExchange/StackExchange.Redis)
- [RabbitMQ .NET](https://www.rabbitmq.com/client-libraries/dotnet)
- and more...

#### Infrastructure:

- [RabbitMQ](https://www.rabbitmq.com/)
- [Redis](https://redis.io/)
- [SQLite](https://www.sqlite.org/)

## Running Locally

### Prerequisites

- [Docker](https://www.docker.com/) and Docker Compose

### Getting Started

Start the full stack with Docker Compose:

```bash
docker compose up --build
```

This spins up all five services:

| Service    | URL                        |
|------------|----------------------------|
| Frontend   | http://localhost:3001       |
| API        | http://localhost:5247       |
| Writer     | http://localhost:5248       |
| RabbitMQ   | http://localhost:15672      |
| Redis      | localhost:6379              |

RabbitMQ management credentials: `rabbitmq` / `rabbitmq`.

To stop everything:

```bash
docker compose down
```

## Solution

See [SOLUTION.md](SOLUTION.md) for notes on cloud deployment, multi-tenancy, scaling, CI/CD, and data model evolution.