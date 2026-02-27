# ASP.NET REST API Development

## Mission

Guide developers through building secure, well-designed REST APIs using ASP.NET Core 10. Provide educational context alongside code examples. Emphasize best practices for API design, testing, documentation, and deployment.

## API Design Fundamentals

### **REST Architecture Principles**

- **Resource-Oriented Design:** Model your API around resources (nouns), not actions (verbs).
- **HTTP Verbs:** Use appropriate HTTP methods:
    - `GET`: Retrieve a resource
    - `POST`: Create a new resource
    - `PUT`/`PATCH`: Update an existing resource
    - `DELETE`: Remove a resource
- **Status Codes:** Use appropriate HTTP status codes
    - `200 OK`: Successful GET, PUT, PATCH
    - `201 Created`: Successful POST
    - `204 No Content`: Successful DELETE
    - `400 Bad Request`: Invalid request data
    - `401 Unauthorized`: Missing authentication
    - `403 Forbidden`: Insufficient permissions
    - `404 Not Found`: Resource doesn't exist
    - `500 Internal Server Error`: Server error
- **URL Design:** Use meaningful, hierarchical URLs
    - Good: `/api/users/123/orders`
    - Bad: `/api/GetUserOrders?id=123`

### **Initial Design Decisions: Controllers vs. Minimal APIs**

**Controller-Based APIs:**

- Well-suited for complex APIs with multiple endpoints.
- Better for team-based development with clear separation of concerns.
- Attributes-based routing provides explicit endpoint definitions.
- Works well with traditional dependency injection.

**Minimal APIs:**

- Great for simple APIs and microservices.
- Less boilerplate code for straightforward scenarios.
- Fluent routing API is concise and readable.
- Newer approach, fewer learning resources than controllers.

## Project Setup and Structure

### **Creating a New ASP.NET Core 10 Web API Project**

```bash
dotnet new webapi -n MyApi -n MyApi
cd MyApi
dotnet add package FluentValidation
dotnet add package Serilog
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer
```

### **Project Organization**

```
MyApi/
  Models/          # Data models and DTOs
  Controllers/     # API controllers (if using controller-based approach)
  Services/        # Business logic
  Data/           # Data access, EF Core context
  Middleware/     # Custom middleware
  Extensions/     # Extension methods
  Program.cs      # Application startup configuration
```

### **Program.cs Configuration**

```csharp
var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IRepository<User>, Repository<User>>();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    });

// Build app
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
```

## Building Controller-Based APIs

### **Creating RESTful Controllers**

```csharp
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(IUserService userService, ILogger<UsersController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<UserDto>> GetUser(int id)
    {
        try
        {
            var user = await _userService.GetUserByIdAsync(id);
            if (user == null)
                return NotFound(new { message = "User not found" });

            return Ok(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user {UserId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while retrieving the user" });
        }
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<UserDto>> CreateUser(CreateUserDto createUserDto)
    {
        var createdUser = await _userService.CreateUserAsync(createUserDto);
        return CreatedAtAction(nameof(GetUser), new { id = createdUser.Id }, createdUser);
    }

    [HttpPut("{id}")]
    [Authorize]
    public async Task<IActionResult> UpdateUser(int id, UpdateUserDto updateUserDto)
    {
        var result = await _userService.UpdateUserAsync(id, updateUserDto);
        if (result == null)
            return NotFound();

        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "AdminPolicy")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var result = await _userService.DeleteUserAsync(id);
        if (!result)
            return NotFound();

        return NoContent();
    }
}
```

### **Attribute Routing**

- Use `[Route]` attribute for customized routes.
- Use `[HttpGet]`, `[HttpPost]`, `[HttpPut]`, `[HttpDelete]` for specific methods.
- Route tokens like `[controller]` and `[action]` generate routes automatically.

### **Model Binding and Validation**

```csharp
[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    [HttpPost]
    public IActionResult CreateProduct([FromBody] CreateProductDto dto)
    {
        // ModelState is automatically validated when using [ApiController]
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        // Process creation
        return CreatedAtAction(nameof(GetProduct), new { id = 1 }, new { id = 1 });
    }

    [HttpGet]
    public IActionResult GetProducts(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        // FromQuery automatically binds query parameters
        return Ok(new { page, pageSize });
    }
}
```

## Data Access Patterns with Entity Framework Core

### **DbContext Configuration**

```csharp
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<Order> Orders { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure entities
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.Property(u => u.Email).IsRequired().HasMaxLength(255);
            entity.HasIndex(u => u.Email).IsUnique();
            entity.HasMany(u => u.Orders).WithOne(o => o.User).HasForeignKey(o => o.UserId);
        });
    }
}
```

### **Repository Pattern**

```csharp
public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(int id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<T> AddAsync(T entity);
    Task<T> UpdateAsync(T entity);
    Task<bool> DeleteAsync(int id);
}

public class Repository<T> : IRepository<T> where T : class
{
    private readonly AppDbContext _context;
    private readonly DbSet<T> _dbSet;

    public Repository(AppDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public async Task<T?> GetByIdAsync(int id) => await _dbSet.FindAsync(id);

    public async Task<IEnumerable<T>> GetAllAsync() => await _dbSet.ToListAsync();

    public async Task<T> AddAsync(T entity)
    {
        _dbSet.Add(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task<T> UpdateAsync(T entity)
    {
        _dbSet.Update(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var entity = await GetByIdAsync(id);
        if (entity == null) return false;

        _dbSet.Remove(entity);
        await _context.SaveChangesAsync();
        return true;
    }
}
```

### **Database Migrations**

```bash
# Create a migration
dotnet ef migrations add AddUserTable

# Apply migrations to database
dotnet ef database update

# Remove last migration
dotnet ef migrations remove
```

## Authentication and Authorization

### **JWT Bearer Token Authentication**

```csharp
public interface IAuthService
{
    Task<(string token, RefreshToken refreshToken)> AuthenticateAsync(LoginDto loginDto);
    Task<string> RefreshTokenAsync(string refreshToken);
}

public class AuthService : IAuthService
{
    private readonly IConfiguration _configuration;
    private readonly IUserService _userService;

    public AuthService(IConfiguration configuration, IUserService userService)
    {
        _configuration = configuration;
        _userService = userService;
    }

    public async Task<(string token, RefreshToken refreshToken)> AuthenticateAsync(LoginDto loginDto)
    {
        var user = await _userService.GetUserByEmailAsync(loginDto.Email);
        if (user == null || !BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid credentials");

        var token = GenerateAccessToken(user);
        var refreshToken = GenerateRefreshToken();

        // Store refresh token
        await _userService.SaveRefreshTokenAsync(user.Id, refreshToken);

        return (token, refreshToken);
    }

    private string GenerateAccessToken(User user)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role)
        };

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private string GenerateRefreshToken()
    {
        var randomNumber = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }
    }
}
```

### **Role-Based Authorization**

```csharp
[Authorize(Roles = "Admin")]
[HttpDelete("{id}")]
public async Task<IActionResult> DeleteUser(int id)
{
    await _userService.DeleteUserAsync(id);
    return NoContent();
}
```

### **Policy-Based Authorization**

```csharp
// In Program.cs
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("AdminPolicy", policy =>
        policy.RequireRole("Admin"))
    .AddPolicy("ManagerOrAdmin", policy =>
        policy.RequireRole("Manager", "Admin"));

// In controller
[Authorize(Policy = "AdminPolicy")]
[HttpDelete("{id}")]
public async Task<IActionResult> DeleteUser(int id)
{
    await _userService.DeleteUserAsync(id);
    return NoContent();
}
```

## Validation and Error Handling

### **Model Validation with Data Annotations**

```csharp
public class CreateUserDto
{
    [Required(ErrorMessage = "Name is required")]
    [StringLength(100, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters")]
    public string Password { get; set; } = string.Empty;
}
```

### **FluentValidation**

```csharp
public class CreateUserDtoValidator : AbstractValidator<CreateUserDto>
{
    private readonly IUserService _userService;

    public CreateUserDtoValidator(IUserService userService)
    {
        _userService = userService;

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .Length(2, 100).WithMessage("Name must be between 2 and 100 characters");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format")
            .MustAsync(async (email, ct) => !await _userService.EmailExistsAsync(email))
            .WithMessage("Email is already in use");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters")
            .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter");
    }
}
```

### **Global Exception Handling Middleware**

```csharp
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var response = new ProblemDetails
        {
            Instance = context.Request.Path
        };

        switch (exception)
        {
            case ValidationException vex:
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                response.Status = StatusCodes.Status400BadRequest;
                response.Title = "Validation Error";
                response.Detail = string.Join(", ", vex.Errors);
                break;
            case UnauthorizedAccessException:
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                response.Status = StatusCodes.Status401Unauthorized;
                response.Title = "Unauthorized";
                break;
            default:
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                response.Status = StatusCodes.Status500InternalServerError;
                response.Title = "Internal Server Error";
                break;
        }

        return context.Response.WriteAsJsonAsync(response);
    }
}

// In Program.cs
app.UseMiddleware<ExceptionHandlingMiddleware>();
```

## API Documentation with Swagger/OpenAPI

### **Configuring Swagger**

```csharp
// In Program.cs
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "My API",
        Version = "v1",
        Description = "A sample API for managing users"
    });

    // Add JWT authentication to Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Please enter token",
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        BearerFormat = "JWT",
        Scheme = "bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] { }
        }
    });
});
```

### **XML Documentation for Endpoints**

```csharp
/// <summary>
/// Gets a user by ID
/// </summary>
/// <param name="id">The user ID</param>
/// <returns>The user if found</returns>
/// <response code="200">User found</response>
/// <response code="404">User not found</response>
[HttpGet("{id}")]
[ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
public async Task<ActionResult<UserDto>> GetUser(int id)
{
    // Implementation
}
```

## Testing REST APIs

### **Unit Testing Controllers with xUnit and Moq**

```csharp
public class UsersControllerTests
{
    private readonly Mock<IUserService> _mockUserService;
    private readonly Mock<ILogger<UsersController>> _mockLogger;
    private readonly UsersController _controller;

    public UsersControllerTests()
    {
        _mockUserService = new Mock<IUserService>();
        _mockLogger = new Mock<ILogger<UsersController>>();
        _controller = new UsersController(_mockUserService.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task GetUser_WithValidId_ReturnsOkResultWithUser()
    {
        // Arrange
        int userId = 1;
        var user = new UserDto { Id = userId, Name = "John Doe", Email = "john@example.com" };
        _mockUserService.Setup(s => s.GetUserByIdAsync(userId))
            .ReturnsAsync(user);

        // Act
        var result = await _controller.GetUser(userId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedUser = Assert.IsType<UserDto>(okResult.Value);
        Assert.Equal(userId, returnedUser.Id);
        _mockUserService.Verify(s => s.GetUserByIdAsync(userId), Times.Once);
    }

    [Fact]
    public async Task GetUser_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        int userId = 999;
        _mockUserService.Setup(s => s.GetUserByIdAsync(userId))
            .ReturnsAsync((UserDto?)null);

        // Act
        var result = await _controller.GetUser(userId);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }
}
```

## Performance Optimization

### **Caching Strategies**

```csharp
// In-Memory Caching
public class UserService : IUserService
{
    private readonly IRepository<User> _repository;
    private readonly IMemoryCache _cache;

    public async Task<UserDto?> GetUserByIdAsync(int id)
    {
        var cacheKey = $"user_{id}";
        if (_cache.TryGetValue(cacheKey, out UserDto? user))
            return user;

        var dbUser = await _repository.GetByIdAsync(id);
        if (dbUser != null)
        {
            var dto = new UserDto { /* map properties */ };
            _cache.Set(cacheKey, dto, TimeSpan.FromMinutes(10));
            return dto;
        }

        return null;
    }
}
```

### **Pagination, Filtering, and Sorting**

```csharp
[HttpGet]
public async Task<ActionResult<PaginatedResult<UserDto>>> GetUsers(
    [FromQuery] int pageNumber = 1,
    [FromQuery] int pageSize = 10,
    [FromQuery] string? sortBy = nameof(UserDto.Name),
    [FromQuery] string? searchTerm = null)
{
    var result = await _userService.GetUsersAsync(
        pageNumber, pageSize, sortBy, searchTerm);
    return Ok(result);
}
```

## Deployment and DevOps

### **Containerization**

```dockerfile
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["MyApi.csproj", "./"]
RUN dotnet restore
COPY . .
RUN dotnet build -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=publish /app/publish .
EXPOSE 80
ENTRYPOINT ["dotnet", "MyApi.dll"]
```

### **Health Checks**

```csharp
// In Program.cs
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>();

app.MapHealthChecks("/health");
```

---

applyTo: '**/\*.cs, **/\*.json'
description: 'Guidelines for building REST APIs with ASP.NET Core 10, covering design principles, project structure, controllers, data access, authentication, validation, documentation, testing, and deployment.'
