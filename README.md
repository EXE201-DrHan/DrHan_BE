# DrHan - Backend API

A comprehensive food management and meal planning API built with .NET 8, featuring secure authentication, role-based authorization, and advanced data management capabilities.

## 🚀 Features

- **🔐 Secure Authentication**: JWT-based authentication with refresh tokens
- **👥 Role-Based Authorization**: Admin and Customer roles with proper access controls
- **📧 Email Services**: User registration confirmation and password reset
- **🗃️ Data Management**: Comprehensive seeding and management tools
- **🏗️ Clean Architecture**: Well-structured, maintainable codebase
- **📊 Database Management**: Entity Framework with migrations
- **🐳 Docker Support**: Containerized deployment ready
- **📝 API Documentation**: Comprehensive endpoint documentation

## 🏛️ Architecture

This project follows **Clean Architecture** principles with clear separation of concerns:

```
DrHan/
├── DrHan.API/                 # Presentation Layer
│   ├── Controllers/           # API Controllers
│   ├── Extensions/           # Service configurations
│   └── Middleware/           # Custom middleware
├── DrHan.Application/        # Application Layer
│   ├── Services/             # Business logic
│   ├── DTOs/                 # Data transfer objects
│   ├── Interfaces/           # Service contracts
│   └── Commons/              # Shared utilities
├── DrHan.Infrastructure/     # Infrastructure Layer
│   ├── ExternalServices/     # External service implementations
│   ├── Persistence/          # Database context
│   ├── Repositories/         # Data access
│   └── Seeders/              # Data seeding
└── DrHan.Domain/            # Domain Layer
    ├── Entities/             # Domain entities
    ├── Constants/            # Domain constants
    └── Exceptions/           # Domain exceptions
```

## 🛠️ Technology Stack

- **Framework**: .NET 8.0
- **Database**: SQL Server with Entity Framework Core
- **Authentication**: ASP.NET Core Identity + JWT
- **Email**: SMTP integration
- **API Documentation**: Swagger/OpenAPI
- **Architecture**: Clean Architecture + CQRS with MediatR
- **Validation**: FluentValidation
- **Logging**: Serilog
- **Containerization**: Docker

## 🔧 Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [SQL Server](https://www.microsoft.com/en-us/sql-server/sql-server-downloads) (LocalDB or full instance)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) or [VS Code](https://code.visualstudio.com/)
- [Docker](https://www.docker.com/) (optional, for containerized deployment)

## ⚡ Quick Start

### 1. Clone the Repository
```bash
git clone https://github.com/your-username/DrHan.git
cd DrHan
```

### 2. Configure Database Connection
Update the connection string in `DrHan/appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=DrHanDb;TrustServerCertificate=true;Trusted_Connection=true;"
  }
}
```

### 3. Configure Email Settings
Update email configuration in `DrHan/appsettings.json`:
```json
{
  "MailSettings": {
    "Mail": "your-email@example.com",
    "DisplayName": "DrHan",
    "Password": "your-app-password",
    "Host": "smtp.gmail.com",
    "Port": 587
  }
}
```

### 4. Database Setup
```bash
# Navigate to the main project directory
cd DrHan

# Run database migrations
dotnet ef database update

# Seed initial data (optional)
dotnet run --launch-profile DrHan.API
```

### 5. Run the Application
```bash
dotnet run --project DrHan
```

The API will be available at:
- **HTTPS**: `https://localhost:7000`
- **HTTP**: `http://localhost:5000`
- **Swagger UI**: `https://localhost:7000/swagger`

## 🔑 Authentication

The API uses JWT (JSON Web Tokens) for authentication with the following token types:

- **Access Token**: 20 minutes expiration, used for API access
- **Refresh Token**: 30 days expiration, used for token renewal

### Quick Authentication Test
```bash
# Register a new user
POST /api/authentication/register
{
  "email": "test@example.com",
  "password": "SecurePassword123!",
  "confirmPassword": "SecurePassword123!",
  "fullName": "Test User",
  "dateOfBirth": "1990-01-01",
  "gender": "Male"
}

# Login (use debug-login for testing without email confirmation)
POST /api/authentication/debug-login
{
  "email": "test@example.com",
  "password": "SecurePassword123!"
}
```

## 📚 API Documentation

Comprehensive API documentation is available:

- **[Authentication API Documentation](./AUTHENTICATION_API.md)** - Complete authentication endpoints
- **[Data Management API Documentation](./DATAMANAGEMENT_API.md)** - Data seeding, cleaning, and monitoring endpoints
- **Swagger UI** - Interactive API explorer at `/swagger` when running locally
- **OpenAPI Specification** - Available at `/swagger/v1/swagger.json`

### Key Endpoints

| Endpoint | Method | Description | Auth Required |
|----------|--------|-------------|---------------|
| `/api/authentication/register` | POST | User registration | ❌ |
| `/api/authentication/login` | POST | User login | ❌ |
| `/api/authentication/profile` | GET | Get user profile | ✅ |
| `/api/authentication/admin-only` | GET | Admin test endpoint | ✅ (Admin) |
| `/api/datamanagement/*` | Various | Data management | ✅ (Admin) |

## 🗄️ Database Management

### Migrations
```bash
# Add new migration
dotnet ef migrations add MigrationName --project DrHan.Infrastructure

# Update database
dotnet ef database update --project DrHan

# Remove last migration
dotnet ef migrations remove --project DrHan.Infrastructure
```

### Data Seeding
The application includes comprehensive data seeding capabilities:

```bash
# Available seeding endpoints (Admin only)
POST /api/datamanagement/seed/all          # Seed all data
POST /api/datamanagement/seed/users        # Seed users and roles
POST /api/datamanagement/seed/food         # Seed food data
POST /api/datamanagement/reset/all         # Reset and reseed all data
```

### Configuration Options
Set these in `appsettings.json` for automatic database management:
```json
{
  "DatabaseSettings": {
    "AutoMigrate": false,    # Automatically run migrations on startup
    "AutoSeed": false        # Automatically seed data on startup
  },
  "ClearAndReseedData": false  # Clear and reseed data on startup
}
```

## 🐳 Docker Deployment

### Build and Run with Docker
```bash
# Build the Docker image
docker build -t drhan-api .

# Run the container
docker run -p 8080:80 -p 8081:443 drhan-api
```

### Docker Compose (if available)
```bash
docker-compose up -d
```

## 🔧 Development

### Project Structure
- **Controllers**: Handle HTTP requests and responses
- **Services**: Implement business logic using CQRS pattern
- **Repositories**: Handle data access
- **DTOs**: Data transfer objects for API communication
- **Entities**: Domain models
- **Extensions**: Service registration and configuration

### Adding New Features
1. **Create Entity** in `DrHan.Domain/Entities/`
2. **Add Repository** in `DrHan.Infrastructure/Repositories/`
3. **Create DTOs** in `DrHan.Application/DTOs/`
4. **Implement Services** in `DrHan.Application/Services/`
5. **Add Controller** in `DrHan.API/Controllers/`
6. **Create Migration** using Entity Framework

### Code Standards
- Follow Clean Architecture principles
- Use CQRS pattern with MediatR
- Implement proper error handling
- Add comprehensive validation
- Include XML documentation for APIs
- Follow async/await patterns

## 🧪 Testing

### Run Tests
```bash
# Run all tests
dotnet test

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"
```

### API Testing
- Use **Swagger UI** for interactive testing
- Import OpenAPI spec into **Postman** or **Insomnia**
- Refer to [Authentication API Documentation](./AUTHENTICATION_API.md) for examples

## 🔒 Security Features

- **JWT Authentication** with secure token generation
- **Password Hashing** using ASP.NET Core Identity
- **Email Confirmation** for new accounts
- **Account Lockout** protection against brute force
- **Role-Based Authorization** (Admin, Customer)
- **Timing Attack Protection** in login process
- **Input Validation** using FluentValidation
- **CORS Configuration** for cross-origin requests

## 📝 Configuration

### Required Environment Variables
```bash
# Database
ConnectionStrings__DefaultConnection="your-connection-string"

# JWT Settings
JwtSettings__SecretKey="your-secret-key-minimum-32-characters"
JwtSettings__Issuer="your-app-name"
JwtSettings__Audience="your-client-app"

# Email Settings
MailSettings__Mail="your-email@example.com"
MailSettings__Password="your-app-password"
```

### appsettings.json Structure
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "..."
  },
  "JwtSettings": {
    "SecretKey": "...",
    "Issuer": "...",
    "Audience": "...",
    "ExpirationInMinutes": 20
  },
  "JwtRefreshTokenSettings": {
    "SecretKey": "...",
    "Issuer": "...",
    "Audience": "...",
    "ExpirationInMinutes": 43200
  },
  "MailSettings": {
    "Mail": "...",
    "DisplayName": "...",
    "Password": "...",
    "Host": "smtp.gmail.com",
    "Port": 587
  },
  "DatabaseSettings": {
    "AutoMigrate": false,
    "AutoSeed": false
  }
}
```

## 🚀 Deployment

### Production Checklist
- [ ] Update connection strings for production database
- [ ] Configure secure JWT secrets
- [ ] Set up email service credentials
- [ ] Enable HTTPS enforcement
- [ ] Configure CORS for production domains
- [ ] Remove debug endpoints (`/debug-login`)
- [ ] Set up logging and monitoring
- [ ] Configure health checks

### Environment-Specific Settings
Create separate `appsettings.{Environment}.json` files:
- `appsettings.Development.json`
- `appsettings.Staging.json`
- `appsettings.Production.json`

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Follow the coding standards and architecture patterns
4. Add tests for new functionality
5. Commit your changes (`git commit -m 'Add amazing feature'`)
6. Push to the branch (`git push origin feature/amazing-feature`)
7. Open a Pull Request

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🆘 Support

- **Documentation**: 
  - [Authentication API Docs](./AUTHENTICATION_API.md)
  - [Data Management API Docs](./DATAMANAGEMENT_API.md)
- **Issues**: [GitHub Issues](https://github.com/your-username/DrHan/issues)
- **Discussions**: [GitHub Discussions](https://github.com/your-username/DrHan/discussions)

## 📈 Roadmap

- [ ] Add comprehensive unit tests
- [ ] Implement integration tests
- [ ] Add health check endpoints
- [ ] Implement real-time notifications
- [ ] Add API rate limiting
- [ ] Implement caching strategies
- [ ] Add OpenTelemetry for observability

---

Built with ❤️ using .NET 8 and Clean Architecture principles.