# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Real-time chat system (similar to WhatsApp/Telegram) built with .NET 10 microservices architecture and React frontend. Academic project for Tecnicatura Universitaria en Programación (UTN). The system implements JWT authentication with BCrypt password hashing, SignalR for bidirectional real-time communication, and Supabase (PostgreSQL) for data persistence.

## Architecture

### Microservices Structure

The project consists of three independent backend microservices plus a React frontend:

- **AuthService** (Port typically 5257)
  - User registration, login, JWT token generation/refresh
  - BCrypt password hashing with `BCrypt.Net.BCrypt`
  - Profile management and user listing
  - Entity Framework Core with PostgreSQL (Supabase)
  - Entities: `Usuario` (usuarios table)
  - DbContext: `AppDbContext`
  - Migrations: `AuthService/Migrations/`
  - Endpoints: Minimal APIs in `AuthService/Endpoints/AuthEndpoints.cs`
  - Auto-migration on startup with seed data (admin@example.com / Admin123!)

- **MessagesService**
  - 1:1 and group messaging with full message history
  - SignalR ChatHub at `/hubs/chat` for real-time events
  - Read receipts ("visto") with timestamp tracking
  - Typing indicators with broadcast to other users
  - Cross-service communication to AuthService via `UserInfoService` for user name resolution
  - Entities: `Conversacion`, `Mensaje`, `MensajeLeido`, `ParticipanteConversacion`, `Grupo`, `GrupoMiembro`
  - DbContext: `MessagesDbContext`
  - Migrations: `MessagesService/Migrations/`
  - Endpoints: Minimal APIs in `MessagesService/Endpoints/MessageEndpoints.cs`
  - SignalR Hub: `MessagesService/Hubs/ChatHub.cs`
  - JWT propagation via HttpContextAccessor for inter-service calls

- **GroupsService**
  - Group creation and management (CRUD operations)
  - Member management (add/remove participants)
  - Group admin authorization checks
  - Entities: `Grupo`, `GrupoMiembro`
  - DbContext: `GroupsDbContext`
  - Migrations: `GroupsService/Migrations/`
  - Endpoints: Minimal APIs in `GroupsService/Endpoints/GroupEndpoints.cs`

- **Shared** - Common library for all services
  - DTOs: `UsuarioDto`, `MessageDto`, `ConversationDto`, `GrupoDto`, `ReadReceiptDto`, `TypingIndicatorDto`
  - `Shared.Responses.ApiResponse<T>` - Standardized API response wrapper

- **realtime-chat-ui/** - React + TypeScript + Vite frontend (Port 5173)
  - SignalR integration via `@microsoft/signalr`
  - Axios for HTTP API calls
  - React Router for SPA routing
  - Zustand for state management (authStore, chatStore)
  - TailwindCSS for styling
  - Lucide React icons
  - Components: Chat windows, message bubbles, typing indicators, read receipts, group management

### Key Design Patterns

- **Microservices**: Each backend service is independently deployable with its own database schema
- **Minimal APIs**: All services use .NET Minimal APIs (not Controller classes) - endpoints defined in `Endpoints/` folders
- **Shared Kernel**: Common DTOs and response types in Shared project to avoid duplication
- **Service Layer**: Business logic in `Services/` folders with interface/implementation pattern (e.g., `IAuthService`/`AuthServiceImpl`)
- **API Response Wrapper**: All API responses use `ApiResponse<T>` with `SuccessResponse()` and `FailureResponse()` factory methods
- **JWT Propagation**: MessagesService forwards JWT to AuthService for cross-service authentication

### Database Architecture

- **Provider**: PostgreSQL via Supabase
- **ORM**: Entity Framework Core 10.0
- **Migrations**: Code-first approach with auto-apply on startup
- **Connection String**: Stored in `appsettings.json` ConnectionStrings:DefaultConnection
- **Table Naming**: Lowercase snake_case convention via `[Table("table_name")]` attribute
- **Multiple Schemas**: Each service has its own migrations and can share the same database or use separate ones
- **Pooler IPv4 Workaround**: Services check for `pooler.supabase.com` in connection string to skip seeding and avoid migration history conflicts

### Authentication & Security

- **JWT Tokens**: Access tokens and refresh tokens fully implemented in AuthService
- **Password Security**: BCrypt hashing via `BCrypt.Net.BCrypt.HashPassword()` and `.Verify()`
- **Email Uniqueness**: Enforced at database level via unique index on `email` column
- **Authorization**: All endpoints require `[Authorize]` or `.RequireAuthorization()` except register/login
- **SignalR Security**: ChatHub validates JWT from query string (`?access_token=...`) via `OnMessageReceived` event in JwtBearerEvents
- **CORS**: Configured for common frontend ports (3000, 5173, 4200, 8080) with credentials support
- **User Claims**: JWT contains `ClaimTypes.NameIdentifier` with user ID for authorization checks

### SignalR Real-Time Features

SignalR ChatHub (`/hubs/chat`) implements:
- **JoinConversation(int conversacionId)** - Adds connection to SignalR group, validates authorization
- **LeaveConversation(int conversacionId)** - Removes connection from SignalR group
- **SendMessage(int conversacionId, string contenido)** - Broadcasts `ReceiveMessage` event to all group members
- **SendTypingIndicator(int conversacionId, bool isTyping)** - Broadcasts `ReceiveTypingIndicator` to others in group (not sender)
- **MarkMessageAsRead(int messageId)** - Persists read receipt and broadcasts `ReceiveReadReceipt` event
- All methods require `[Authorize]` and validate user participation before broadcasting

## Development Commands

### Backend Services

```bash
# Build entire solution
dotnet build RealTimeChat/RealTimeChat.slnx

# Build specific service
dotnet build RealTimeChat/AuthService/AuthService.csproj
dotnet build RealTimeChat/MessagesService/MessagesService.csproj
dotnet build RealTimeChat/GroupsService/GroupsService.csproj

# Run services (each in separate terminal)
dotnet run --project RealTimeChat/AuthService/AuthService.csproj
dotnet run --project RealTimeChat/MessagesService/MessagesService.csproj
dotnet run --project RealTimeChat/GroupsService/GroupsService.csproj
```

### Frontend

```bash
# Navigate to frontend folder
cd realtime-chat-ui

# Install dependencies
npm install

# Run development server (http://localhost:5173)
npm run dev

# Build for production
npm run build

# Preview production build
npm run preview

# Lint code
npm run lint
```

### Database Migrations

Each service has its own migrations. Replace `{Service}` with AuthService, MessagesService, or GroupsService:

```bash
# Create new migration
dotnet ef migrations add MigrationName --project RealTimeChat/{Service}

# Apply migrations to database (also happens automatically on service startup)
dotnet ef database update --project RealTimeChat/{Service}

# Remove last migration (if not applied)
dotnet ef migrations remove --project RealTimeChat/{Service}

# List all migrations
dotnet ef migrations list --project RealTimeChat/{Service}
```

### Testing

No test projects currently exist. When adding tests:

```bash
# Run all tests
dotnet test

# Run specific test project
dotnet test path/to/TestProject.csproj
```

## Project Requirements (from Integrador doc)

### Functional Requirements Summary

1. **REQ-01**: User registration/login with email validation, JWT tokens, profile management ✅ IMPLEMENTED
2. **REQ-02**: Auth API endpoints for registration, login, profile CRUD, JWT issuance ✅ IMPLEMENTED
3. **REQ-03**: Messages API with pagination, "typing..." SignalR events, read receipts ✅ IMPLEMENTED
4. **REQ-04**: Groups API for creation, deletion, member management ✅ IMPLEMENTED
5. **REQ-05**: 1:1 direct messaging with real-time delivery and message history ✅ IMPLEMENTED
6. **REQ-06**: Group chats with multi-participant support ✅ IMPLEMENTED
7. **REQ-07**: Real-time "typing..." indicators using SignalR (hide after 3s or on send) ✅ IMPLEMENTED
8. **REQ-08**: Read receipts with timestamp persistence and visual indicators ✅ IMPLEMENTED
9. **REQ-09**: View list of users who read a message with read timestamps ✅ IMPLEMENTED
10. **REQ-10**: JWT auth on all endpoints, SignalR token validation, resource-based authorization, password hashing, CORS configuration ✅ IMPLEMENTED
11. **REQ-11**: Functional UI (React/Vue/Angular/Blazor/vanilla) for login, chat list, messaging, typing/read indicators, group creation ✅ IMPLEMENTED (React)

### Technical Constraints

- .NET 9 or higher ✅ Using .NET 10
- SignalR for WebSocket communication ✅ IMPLEMENTED
- PostgreSQL database (via Supabase) ✅ IMPLEMENTED
- JWT authentication ✅ IMPLEMENTED
- Microservices must be separate projects ✅ IMPLEMENTED

## Current Implementation Status

### Completed Features

- ✅ Full AuthService with registration, login, JWT generation/refresh, profile management
- ✅ Full MessagesService with real-time messaging, conversation management, read receipts
- ✅ Full GroupsService with group CRUD and member management
- ✅ SignalR ChatHub with all required events (join, leave, send, typing, read receipts)
- ✅ JWT authentication and authorization on all protected endpoints
- ✅ BCrypt password hashing
- ✅ CORS configuration for frontend origins
- ✅ Database migrations for all services
- ✅ React frontend with SignalR integration, routing, state management
- ✅ Frontend components for chat, groups, authentication, UI elements
- ✅ Auto-migration on service startup

### Architecture Highlights

- **Service Communication**: MessagesService calls AuthService via HTTP to get user names (JWT propagated via `IHttpContextAccessor`)
- **SignalR Groups**: Uses `$"conversation_{conversacionId}"` pattern for broadcasting to conversation participants
- **Minimal APIs**: No Controller classes - all endpoints use `.MapPost()`, `.MapGet()`, etc. in `Endpoints/` files
- **Configuration**: JWT settings in `Configuration/JwtSettings.cs` bound from `appsettings.json`
- **Dependency Injection**: All services registered in `Program.cs` with `AddScoped`, `AddHttpClient`, `AddSignalR`
- **Swagger**: Enabled in development mode at `/swagger` endpoint for all services

## Code Organization

### Backend Structure

```
RealTimeChat/
├── AuthService/
│   ├── Configuration/JwtSettings.cs
│   ├── Data/AppDbContext.cs
│   ├── DTOs/ (service-specific DTOs)
│   ├── Endpoints/AuthEndpoints.cs (Minimal API definitions)
│   ├── Entities/Usuario.cs
│   ├── Migrations/
│   ├── Services/IAuthService.cs, AuthServiceImpl.cs
│   └── Program.cs
├── MessagesService/
│   ├── Configuration/JwtSettings.cs
│   ├── Data/MessagesDbContext.cs
│   ├── DTOs/ (service-specific DTOs)
│   ├── Endpoints/MessageEndpoints.cs
│   ├── Entities/ (Conversacion, Mensaje, MensajeLeido, etc.)
│   ├── Hubs/ChatHub.cs (SignalR hub)
│   ├── Migrations/
│   ├── Services/IMessageService.cs, MessageServiceImpl.cs, IUserInfoService.cs, UserInfoService.cs
│   └── Program.cs
├── GroupsService/
│   ├── Configuration/JwtSettings.cs
│   ├── Data/GroupsDbContext.cs
│   ├── DTOs/ (service-specific DTOs)
│   ├── Endpoints/GroupEndpoints.cs
│   ├── Entities/ (Grupo, GrupoMiembro)
│   ├── Migrations/
│   ├── Services/IGroupService.cs, GroupServiceImpl.cs
│   └── Program.cs
└── Shared/
    ├── DTOs/ (shared across all services)
    └── Responses/ApiResponse.cs
```

### Frontend Structure

```
realtime-chat-ui/
├── src/
│   ├── components/
│   │   ├── auth/ (login, register)
│   │   ├── chat/ (ChatWindow, MessageBubble, MessageInput, TypingIndicator, ReadReceiptsList, NewChatModal)
│   │   ├── groups/ (CreateGroupModal, AddMemberModal, MembersList)
│   │   └── layout/ (Sidebar, PrivateRoute)
│   ├── services/ (API clients: api.ts, authService.ts, messageService.ts, groupService.ts)
│   ├── store/ (Zustand stores: authStore.ts, chatStore.ts)
│   └── main.tsx (entry point)
├── package.json
├── vite.config.ts
└── tsconfig.json
```

## Configuration Management

### Backend Configuration

- **Database Connection Strings**: `appsettings.json` → `ConnectionStrings:DefaultConnection`
- **JWT Settings**: `appsettings.json` → `JwtSettings` (SecretKey, Issuer, Audience, ExpiresInMinutes, RefreshTokenExpiresInDays)
- **User Secrets**: Configured in all services for sensitive data (use `dotnet user-secrets set "JwtSettings:SecretKey" "your-secret-key"`)
- **CORS Origins**: Hardcoded in `Program.cs` (ports 3000, 5173, 4200, 8080)

### Frontend Configuration

- **API Base URLs**: Hardcoded in `src/services/api.ts` (AuthService: port 5257, MessagesService, GroupsService)
- **SignalR Hub URL**: Configured in chat services (http://localhost:{port}/hubs/chat)
- **Environment**: Vite environment variables can be added in `.env` files

## Common Patterns

### Minimal API Endpoint Pattern

```csharp
public static class SomeEndpoints
{
    public static void MapSomeEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/some").WithTags("SomeTag");

        group.MapPost("/endpoint", async (RequestDto request, IService service) =>
        {
            var result = await service.DoSomething(request);

            if (!result.Success)
                return Results.BadRequest(ApiResponse<ResponseDto>.FailureResponse(result.Message));

            return Results.Ok(ApiResponse<ResponseDto>.SuccessResponse(result, "Success message"));
        })
        .RequireAuthorization() // For protected endpoints
        .WithName("EndpointName")
        .WithOpenApi();
    }
}
```

### API Response Pattern

Always use `ApiResponse<T>` wrapper for consistency:

```csharp
// Success
return Results.Ok(ApiResponse<UsuarioDto>.SuccessResponse(user, "Usuario creado exitosamente"));

// Failure with errors
return Results.BadRequest(ApiResponse<UsuarioDto>.FailureResponse("Error message", errorList));
```

### Entity Framework Pattern

- DbContext per service with its own entities and migrations
- Table names use lowercase snake_case: `[Table("usuarios")]`
- Unique constraints configured in `OnModelCreating`: `builder.HasIndex(u => u.Email).IsUnique()`
- Foreign keys with cascade delete or restrict based on business rules
- Auto-migration on startup via `context.Database.MigrateAsync()`

### SignalR Pattern

- Hub methods extract user ID from `ClaimTypes.NameIdentifier`
- Authorization check before broadcasting: `await _messageService.IsUserInConversationAsync(conversacionId, userId)`
- SignalR groups named with pattern: `$"conversation_{conversacionId}"`
- Broadcast to group: `await Clients.Group(groupName).SendAsync("EventName", dto)`
- Broadcast to others (not sender): `await Clients.OthersInGroup(groupName).SendAsync(...)`

### DTO Pattern

- Shared DTOs in `Shared/DTOs/` for inter-service communication
- Service-specific DTOs in each service's `DTOs/` folder
- Never expose password fields in DTOs
- Use string for IDs in DTOs to support different ID types (int, Guid)
- DTOs match expected frontend structure

## Environment & Dependencies

### Backend Dependencies

- **Target Framework**: net10.0
- **Key NuGet Packages**:
  - Microsoft.AspNetCore.OpenApi 10.0.0
  - Npgsql.EntityFrameworkCore.PostgreSQL 10.0.0
  - Microsoft.EntityFrameworkCore.Design 10.0.0
  - Microsoft.EntityFrameworkCore.Tools 10.0.0
  - Microsoft.AspNetCore.Authentication.JwtBearer 10.0.0
  - BCrypt.Net-Next (for password hashing)
  - Microsoft.AspNetCore.SignalR (MessagesService)
- **Database**: Supabase PostgreSQL (URL: ybgdvvgvatlscyjymnqi.supabase.co)
- **IDE**: Visual Studio 2022 or Rider (project uses .slnx format)

### Frontend Dependencies

- **Runtime**: Node.js (latest LTS)
- **Framework**: React 19.2.0 with TypeScript
- **Build Tool**: Vite 7.2.4
- **Key Libraries**:
  - @microsoft/signalr 10.0.0 (WebSocket communication)
  - axios 1.13.2 (HTTP client)
  - react-router-dom 7.9.6 (routing)
  - zustand 5.0.8 (state management)
  - tailwindcss 3.4.17 (styling)
  - lucide-react 0.555.0 (icons)
  - date-fns 4.1.0 (date formatting)

## Service Communication

### AuthService → MessagesService

MessagesService's `UserInfoService` calls AuthService's `/api/auth/users/{id}` endpoint to resolve user names for messages and typing indicators. JWT token is propagated via `IHttpContextAccessor`.

### Frontend → Backend

- HTTP calls via Axios to individual service endpoints
- SignalR connection to MessagesService ChatHub at `/hubs/chat`
- JWT token stored in authStore (Zustand) and sent in Authorization header
- SignalR receives JWT via query string parameter for WebSocket upgrade
