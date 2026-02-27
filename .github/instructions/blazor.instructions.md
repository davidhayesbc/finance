# Blazor Component Development

## Mission

Guide developers in building efficient, secure, and maintainable Blazor applications. Provide best practices for component development, state management, performance optimization, and testing. Focus on modern C# features and idiomatic patterns.

## Blazor Code Style and Structure

### **Core Principles**

- Write idiomatic and efficient Blazor and C# code.
- Follow .NET and Blazor conventions.
- Use Razor Components appropriately for component-based UI development.
- Prefer inline functions for smaller components; separate complex logic into code-behind or service classes.
- Use async/await for non-blocking UI operations.

### **Naming Conventions**

- **Components:** PascalCase (e.g., `UserProfile.razor`, `OrderList.razor`)
- **Methods:** PascalCase (e.g., `GetUsers()`, `HandleClick()`)
- **Private fields:** camelCase (e.g., `_userId`, `_isLoading`)
- **Local variables:** camelCase (e.g., `userName`, `isValid`)
- **Interfaces:** "I" prefix (e.g., `IUserService`, `IRepository<T>`)
- **Parameters:** camelCase in component parameters

### **Modern C# Features**

- Use C# 13 features: record types, pattern matching, global usings, nullable reference types.
- Leverage expression-bodied members for simple operations.
- Use discards (`_`) for unused variables.
- Use init-only properties for immutable objects.

**Example:**

```csharp
// Modern C# component code
public partial class UserCard : ComponentBase
{
    [Parameter]
    public required User User { get; set; }  // Required parameter

    [Parameter]
    public EventCallback<User> OnUserSelected { get; set; }

    private string UserDisplayName => $"{User.FirstName} {User.LastName}";

    private async Task SelectUserAsync()
    {
        await OnUserSelected.InvokeAsync(User);
    }
}
```

## Component Lifecycle and Initialization

### **Lifecycle Methods**

```csharp
public partial class DataTable : ComponentBase
{
    private List<Item> _items = new();
    private bool _isLoading;

    // Called when component is initialized
    protected override void OnInitialized()
    {
        // Synchronous operations only
        _isLoading = true;
    }

    // Called when component is initialized (async version)
    protected override async Task OnInitializedAsync()
    {
        _items = await LoadItemsAsync();
        _isLoading = false;
    }

    // Called when parameters change
    protected override async Task OnParametersSetAsync()
    {
        if (ShouldReload)
        {
            _items = await LoadItemsAsync();
        }
    }

    // Called after rendering
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            // Initialize JavaScript interop, set up event listeners, etc.
            await JS.InvokeVoidAsync("initializeComponent", ElementReference);
        }
    }

    // Control rendering optimization
    protected override bool ShouldRender()
    {
        // Only render if items changed
        return _itemsChanged;
    }
}
```

## Data Binding and Event Handling

### **Two-Way Binding**

```razor
@page "/user-form"

<input @bind="user.Name" />
<input @bind:event="oninput" @bind="user.Email" />
<textarea @bind="user.Bio"></textarea>

<select @bind="user.RoleId">
    @foreach (var role in roles)
    {
        <option value="@role.Id">@role.Name</option>
    }
</select>

@code {
    private User user = new();
    private List<Role> roles = new();

    protected override async Task OnInitializedAsync()
    {
        roles = await roleService.GetAllAsync();
    }
}
```

### **Event Handling**

```csharp
public partial class SearchUsers : ComponentBase
{
    private string _searchTerm = string.Empty;
    private List<UserDto> _searchResults = new();

    // OnInput event for real-time search
    private async Task OnSearchInputAsync(ChangeEventArgs e)
    {
        _searchTerm = e.Value?.ToString() ?? string.Empty;
        if (_searchTerm.Length >= 2)
        {
            _searchResults = await _userService.SearchAsync(_searchTerm);
        }
        else
        {
            _searchResults.Clear();
        }
    }

    // EventCallback for parent-child communication
    [Parameter]
    public EventCallback<UserDto> OnUserSelected { get; set; }

    private async Task SelectUserAsync(UserDto user)
    {
        await OnUserSelected.InvokeAsync(user);
    }
}
```

## Component Communication

### **Parent-to-Child Communication via Parameters**

```razor
@* Parent component *@
<UserProfile User="currentUser" />

@code {
    private User currentUser = new();
}

@* Child component (UserProfile.razor) *@
@page "/user/{userId:int}"

<div>@User.Name</div>

@code {
    [Parameter]
    public required User User { get; set; }

    [Parameter]
    public int UserId { get; set; }
}
```

### **Child-to-Parent Communication via EventCallback**

```razor
@* Parent component *@
<UserForm OnUserCreated="HandleUserCreated" />

<p>@message</p>

@code {
    private string message = string.Empty;

    private async Task HandleUserCreated(User user)
    {
        message = $"User {user.Name} created successfully!";
        // Handle creation
    }
}

@* Child component (UserForm.razor) *@
<form>
    <input @bind="newUser.Name" />
    <button @onclick="SubmitAsync">Create User</button>
</form>

@code {
    [Parameter]
    public EventCallback<User> OnUserCreated { get; set; }

    private User newUser = new();

    private async Task SubmitAsync()
    {
        await _userService.CreateAsync(newUser);
        await OnUserCreated.InvokeAsync(newUser);
    }
}
```

### **Cascading Parameters**

```razor
@* Parent component *@
<CascadingValue Value="currentTheme">
    <ChildComponent />
</CascadingValue>

@code {
    private Theme currentTheme = Theme.Light;
}

@* Child component *@
<div class="container @currentTheme">
    Content
</div>

@code {
    [CascadingParameter]
    public required Theme CurrentTheme { get; set; }
}
```

## State Management

### **Component State Management**

```csharp
// Simple state container
public class AppState
{
    private User? _currentUser;
    public User? CurrentUser
    {
        get => _currentUser;
        set
        {
            _currentUser = value;
            NotifyStateChanged();
        }
    }

    public event Action? OnChange;

    private void NotifyStateChanged() => OnChange?.Invoke();
}

// Register in Program.cs
builder.Services.AddScoped<AppState>();

// Use in component
[Inject]
private AppState AppState { get; set; } = null!;

protected override void OnInitialized()
{
    AppState.OnChange += StateHasChanged;
}

public void Dispose()
{
    AppState.OnChange -= StateHasChanged;
}
```

### **Advanced State Management with Fluxor**

```csharp
// Install: dotnet add package Fluxor.Blazor.Web

// Define state
[FeatureState]
public class UserState
{
    public IReadOnlyList<User> Users { get; init; } = new List<User>();
    public bool IsLoading { get; init; }
}

// Define actions
public record FetchUsersAction;
public record FetchUsersSuccessAction(List<User> Users);

// Define reducers
[ReducerMethod]
public static UserState ReduceFetchUsers(UserState state, FetchUsersAction _)
    => state with { IsLoading = true };

[ReducerMethod]
public static UserState ReduceFetchUsersSuccess(UserState state, FetchUsersSuccessAction action)
    => state with { Users = action.Users, IsLoading = false };

// Use in component
[Inject]
private IDispatcher Dispatcher { get; set; } = null!;

[Inject]
private IState<UserState> State { get; set; } = null!;

protected override void OnInitialized()
{
    Dispatcher.Dispatch(new FetchUsersAction());
}
```

## Performance Optimization

### **Minimizing Render Tree**

```csharp
// BAD: Causes unnecessary re-renders
public partial class ParentComponent : ComponentBase
{
    private int count;

    private void Increment() => count++;

    // Renders entire component tree even when count doesn't change
}

// GOOD: Use StateHasChanged() only when necessary
public partial class ParentComponent : ComponentBase
{
    private int count;

    private async Task IncrementAsync()
    {
        count++;
        // Only trigger re-render if necessary
        await InvokeAsync(StateHasChanged);
    }

    protected override bool ShouldRender()
    {
        // Prevent unnecessary renders
        return _hasChanged;
    }
}
```

### **Using @key Directive**

```razor
@foreach (var item in items)
{
    @* Only re-render when item reference changes *@
    <ItemComponent @key="item.Id" Item="item" />
}
```

### **Virtualization for Large Lists**

```razor
<Virtualize Items="largeItemList" Context="item">
    <ItemTemplate>
        <div>@item.Name</div>
    </ItemTemplate>
</Virtualize>

@code {
    private List<Item> largeItemList = new();
}
```

## Form Handling and Validation

### **EditForm with FluentValidation**

```razor
<EditForm Model="user" OnValidSubmit="HandleValidSubmit">
    <DataAnnotationsValidator />
    <ValidationMessage For="@(() => user.Name)" />

    <InputText id="name" @bind-Value="user.Name" />

    <InputSelect id="role" @bind-Value="user.RoleId">
        @foreach (var role in roles)
        {
            <option value="@role.Id">@role.Name</option>
        }
    </InputSelect>

    <button type="submit">Submit</button>
</EditForm>

@code {
    private User user = new();
    private List<Role> roles = new();

    private async Task HandleValidSubmit()
    {
        await userService.SaveAsync(user);
    }
}
```

### **Custom Validation**

```csharp
public class UniqueEmailValidator : ComponentBase
{
    [Parameter]
    public required string Email { get; set; }

    public bool IsValid { get; protected set; }

    protected override async Task OnParametersSetAsync()
    {
        IsValid = await _userService.IsEmailUniqueAsync(Email);
    }
}
```

## Caching Strategies

### **In-Memory Caching in Blazor Server**

```csharp
[Inject]
private IMemoryCache MemoryCache { get; set; } = null!;

private async Task<List<User>> GetUsersAsync()
{
    var cacheKey = "users_all";
    if (!MemoryCache.TryGetValue(cacheKey, out List<User>? users))
    {
        users = await _userService.GetAllAsync();
        var cacheOptions = new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(10));
        MemoryCache.Set(cacheKey, users, cacheOptions);
    }
    return users!;
}
```

### **Local Storage in Blazor WebAssembly**

```csharp
[Inject]
private Blazored.LocalStorage.ILocalStorageService LocalStorage { get; set; } = null!;

private async Task SaveUserPreferencesAsync(UserPreferences preferences)
{
    await LocalStorage.SetItemAsync("userPreferences", preferences);
}

private async Task<UserPreferences> LoadUserPreferencesAsync()
{
    return await LocalStorage.GetItemAsync<UserPreferences>("userPreferences")
        ?? new UserPreferences();
}
```

## Error Handling and Validation

### **Error Boundaries**

```razor
<ErrorBoundary>
    <ChildContent>
        <DataTable Items="items" />
    </ChildContent>
    <ErrorContent Context="ex">
        <div class="alert alert-danger">
            An error occurred: @ex.Message
        </div>
    </ErrorContent>
</ErrorBoundary>
```

### **Try-Catch in Async Operations**

```csharp
private async Task LoadDataAsync()
{
    try
    {
        _isLoading = true;
        _data = await _dataService.GetDataAsync();
    }
    catch (ApiException ex)
    {
        _errorMessage = $"Failed to load data: {ex.Message}";
        _logger.LogError(ex, "Data loading failed");
    }
    finally
    {
        _isLoading = false;
    }
}
```

## Testing Blazor Components

### **Unit Testing with bUnit**

```csharp
public class UserCardComponentTests
{
    [Fact]
    public void UserCard_DisplaysUserName()
    {
        // Arrange
        var user = new User { Id = 1, Name = "John Doe", Email = "john@example.com" };
        using var context = new Bunit.TestContext();

        // Act
        var component = context.RenderComponent<UserCard>(
            parameters => parameters.Add(p => p.User, user));

        // Assert
        component.Find("h2").TextContent.Should().Contain("John Doe");
    }

    [Fact]
    public async Task UserCard_OnUserSelected_CallsCallback()
    {
        // Arrange
        var user = new User { Id = 1, Name = "John Doe" };
        using var context = new Bunit.TestContext();
        var callbackInvoked = false;

        // Act
        var component = context.RenderComponent<UserCard>(
            parameters =>
            {
                parameters.Add(p => p.User, user);
                parameters.Add(p => p.OnUserSelected, EventCallback.Factory.Create<User>(this, _ => callbackInvoked = true));
            });

        await component.InvokeAsync(async () =>
        {
            var button = component.Find("button");
            await button.ClickAsync(new MouseEventArgs());
        });

        // Assert
        callbackInvoked.Should().BeTrue();
    }
}
```

## Security Best Practices

### **Authentication with Blazor**

```csharp
// Register authentication in Program.cs
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>();
builder.Services.AddAuthorizationCore();
```

### **Authorization Directives**

```razor
<AuthorizeView>
    <Authorized>
        <p>Welcome, @context.User.Identity?.Name</p>
    </Authorized>
    <NotAuthorized>
        <p>Please log in.</p>
    </NotAuthorized>
</AuthorizeView>

<AuthorizeView Roles="Admin">
    <p>This is only visible to admins.</p>
</AuthorizeView>
```

### **HTTPS and CORS**

```csharp
// In Program.cs
app.UseHttpsRedirection();
app.UseCors("BlazorCorsPolicy");

builder.Services.AddCors(options =>
{
    options.AddPolicy("BlazorCorsPolicy", builder =>
    {
        builder.WithOrigins("https://localhost:7001")
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});
```

## API Integration

### **HttpClient Usage**

```csharp
[Inject]
private HttpClient Http { get; set; } = null!;

private List<User> users = new();

protected override async Task OnInitializedAsync()
{
    try
    {
        users = await Http.GetFromJsonAsync<List<User>>("api/users") ?? new();
    }
    catch (HttpRequestException ex)
    {
        _logger.LogError(ex, "Failed to fetch users");
        _errorMessage = "Failed to load users";
    }
}

private async Task CreateUserAsync(User user)
{
    var response = await Http.PostAsJsonAsync("api/users", user);
    if (response.IsSuccessStatusCode)
    {
        var createdUser = await response.Content.ReadAsAsync<User>();
        users.Add(createdUser);
    }
}
```

## JS Interop

### **Calling JavaScript from C#**

```csharp
[Inject]
private IJSRuntime JS { get; set; } = null!;

private async Task ShowAlertAsync(string message)
{
    await JS.InvokeVoidAsync("showAlert", message);
}

private async Task<string> GetUserInputAsync(string prompt)
{
    return await JS.InvokeAsync<string>("getUserInput", prompt);
}
```

### **Calling C# from JavaScript**

```javascript
// In your JavaScript file
window.interopFunctions = {
    showAlert: function (message) {
        alert(message);
    },
};
```

---

applyTo: '**/\*.razor, **/_.razor.cs, \*\*/_.razor.css'
description: 'Best practices for Blazor component development, including component lifecycle, data binding, state management, performance optimization, form handling, testing, and security.'
