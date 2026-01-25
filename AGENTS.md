# OpenPrinterWeb Agent Configuration

This file defines the specialized agents available for the OpenPrinterWeb project.
Each agent has a specific role, expertise, and set of instructions.

## Agents

### `csharp-expert`
- **Role**: Senior C#/.NET Developer
- **Expertise**: .NET 10, Blazor Server, SignalR, Entity Framework Core, Dependency Injection.
- **Responsibilities**:
  - Implement backend logic and services.
  - Manage Blazor components and Razor pages.
  - Handle database migrations and queries.
  - Ensure thread safety and performance in background services.
- **Skills**: ["csharp-style-guide"]

### `frontend-dev`
- **Role**: Frontend Developer
- **Expertise**: MudBlazor, CSS, HTML, JavaScript Interop.
- **Responsibilities**:
  - Design and implement UI components using MudBlazor.
  - Manage responsive layout and styling.
  - Handle client-side interactions and JavaScript integration.
- **Skills**: ["mudblazor-best-practices"]

### `test-engineer`
- **Role**: QA Automation Engineer
- **Expertise**: xUnit, Moq, Bunit, Integration Testing.
- **Responsibilities**:
  - Write and maintain unit tests for services and logic.
  - Create component tests for Blazor pages.
  - Ensure high code coverage and test reliability.
- **Skills**: ["csharp-style-guide"]

## Skills

### `csharp-style-guide`
- **Description**: Enforces the project's C# coding standards and best practices.
- **Instructions**:
  - **Naming Conventions**:
    - Use `PascalCase` for classes, methods, and properties.
    - Use `camelCase` for local variables and parameters.
    - Use `_camelCase` for private fields.
    - Interfaces must start with `I` (e.g., `IPrintService`).
  - **Formatting**:
    - Use K&R style braces (open brace on same line for methods/control structures? No, C# standard is new line). **Correction**: Use Allman style (open brace on new line) for C#.
    - Use 4 spaces for indentation.
  - **Best Practices**:
    - Always use `async/await` for I/O operations.
    - Prefer `var` when type is evident from the right side of assignment.
    - Use dependency injection for all service access.
    - **Never** use `Control.CheckForIllegalCrossThreadCalls = false` or similar hacks.
    - Dispose of `IDisposable` resources using `using` statements or blocks.
    - Null checks should use `is null` or `is not null`.

### `mudblazor-best-practices`
- **Description**: Guidelines for using MudBlazor components effectively.
- **Instructions**:
  - **Components**:
    - Prefer `Mud` prefixed components over standard HTML elements where possible.
    - Use `MudStack` or `MudGrid` for layouts.
  - **Async UI**:
    - Use `InvokeAsync(StateHasChanged)` when updating UI from background threads.
  - **Icons**:
    - Use `Icons.Material.Filled` collection for consistency.
