#!/bin/bash
set -euo pipefail

ROOT_DIR="/Users/scott.dickerson/source/repos/stuff-tracker"
cd "$ROOT_DIR"

echo "Adding dependencies and configuring build/test tooling..."

# Add packages to StuffTracker.Api
echo "Adding Hot Chocolate and EF Core packages to StuffTracker.Api..."
# Remove EF Core packages if they were added with wrong version
dotnet remove StuffTracker.Api/StuffTracker.Api.csproj package Microsoft.EntityFrameworkCore 2>/dev/null || true
dotnet remove StuffTracker.Api/StuffTracker.Api.csproj package Microsoft.EntityFrameworkCore.Design 2>/dev/null || true
dotnet remove StuffTracker.Api/StuffTracker.Api.csproj package Pomelo.EntityFrameworkCore.MySql 2>/dev/null || true
# Add Hot Chocolate packages (if not already present, they are)
dotnet add StuffTracker.Api/StuffTracker.Api.csproj package HotChocolate.AspNetCore 2>/dev/null || true
dotnet add StuffTracker.Api/StuffTracker.Api.csproj package HotChocolate.Data 2>/dev/null || true
dotnet add StuffTracker.Api/StuffTracker.Api.csproj package HotChocolate.Data.EntityFramework 2>/dev/null || true
dotnet add StuffTracker.Api/StuffTracker.Api.csproj package HotChocolate.Types.Analyzers 2>/dev/null || true
# Add EF Core packages with correct versions
dotnet add StuffTracker.Api/StuffTracker.Api.csproj package Microsoft.EntityFrameworkCore --version 8.0.11
dotnet add StuffTracker.Api/StuffTracker.Api.csproj package Microsoft.EntityFrameworkCore.Design --version 8.0.11
dotnet add StuffTracker.Api/StuffTracker.Api.csproj package Pomelo.EntityFrameworkCore.MySql --version 8.0.2
echo "✓ Packages added to Api project"

# Add missing packages to StuffTracker.Tests
echo "Adding test tooling packages to StuffTracker.Tests..."
# xunit, xunit.runner.visualstudio, and Microsoft.NET.Test.Sdk already present
dotnet add StuffTracker.Tests/StuffTracker.Tests.csproj package Microsoft.AspNetCore.Mvc.Testing --version 8.0.11
dotnet add StuffTracker.Tests/StuffTracker.Tests.csproj package FluentAssertions
echo "✓ Packages added to Tests project"

# Create Directory.Build.props
echo "Creating Directory.Build.props..."
cat > Directory.Build.props << 'EOF'
<Project>
  <PropertyGroup>
    <Nullable>enable</Nullable>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  </PropertyGroup>
</Project>
EOF
echo "✓ Directory.Build.props created"

# Install dotnet-ef tool (optional)
echo "Installing dotnet-ef tool..."
if [ ! -f ".config/dotnet-tools.json" ]; then
    dotnet new tool-manifest
fi
dotnet tool install dotnet-ef --version latest 2>/dev/null || dotnet tool update dotnet-ef --version latest 2>/dev/null
echo "✓ dotnet-ef tool installed/updated"

# Restore and build solution
echo "Restoring and building solution..."
dotnet restore StuffTracker.sln
dotnet build StuffTracker.sln --no-restore --nologo
echo "✓ Solution restored and built successfully"

# Commit changes
echo "Committing changes..."
git add .
if ! git diff --cached --quiet; then
    git commit -m "Add Hot Chocolate, EF Core MySQL, and test tooling (Task 1.2)"
    echo "✓ Changes committed"
else
    echo "✓ No changes to commit"
fi

echo ""
echo "Task 1.2 dependency setup complete!"

