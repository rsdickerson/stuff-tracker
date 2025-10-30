#!/bin/bash
set -euo pipefail

ROOT_DIR="/Users/scott.dickerson/source/repos/stuff-tracker"
cd "$ROOT_DIR"

echo "Creating .NET solution and projects..."

# Create solution
if [ ! -f "StuffTracker.sln" ]; then
    dotnet new sln -n StuffTracker
    echo "✓ Created StuffTracker.sln"
else
    echo "✓ StuffTracker.sln already exists"
fi

# Create projects (using default framework, then update to net8.0)
if [ ! -f "StuffTracker.Domain/StuffTracker.Domain.csproj" ]; then
    dotnet new classlib -n StuffTracker.Domain -o StuffTracker.Domain
    # Update target framework to net8.0
    sed -i '' 's|<TargetFramework>net9.0</TargetFramework>|<TargetFramework>net8.0</TargetFramework>|g' StuffTracker.Domain/StuffTracker.Domain.csproj
    echo "✓ Created StuffTracker.Domain project (net8.0)"
else
    echo "✓ StuffTracker.Domain already exists"
fi

if [ ! -f "StuffTracker.Api/StuffTracker.Api.csproj" ]; then
    dotnet new web -n StuffTracker.Api -o StuffTracker.Api
    # Update target framework to net8.0
    sed -i '' 's|<TargetFramework>net9.0</TargetFramework>|<TargetFramework>net8.0</TargetFramework>|g' StuffTracker.Api/StuffTracker.Api.csproj
    echo "✓ Created StuffTracker.Api project (net8.0)"
else
    echo "✓ StuffTracker.Api already exists"
fi

if [ ! -f "StuffTracker.Tests/StuffTracker.Tests.csproj" ]; then
    dotnet new xunit -n StuffTracker.Tests -o StuffTracker.Tests
    # Update target framework to net8.0
    sed -i '' 's|<TargetFramework>net9.0</TargetFramework>|<TargetFramework>net8.0</TargetFramework>|g' StuffTracker.Tests/StuffTracker.Tests.csproj
    echo "✓ Created StuffTracker.Tests project (net8.0)"
else
    echo "✓ StuffTracker.Tests already exists"
fi

# Add projects to solution (idempotent - won't error if already added)
echo "Adding projects to solution..."
dotnet sln StuffTracker.sln add StuffTracker.Domain/StuffTracker.Domain.csproj 2>/dev/null || echo "  Domain already in solution"
dotnet sln StuffTracker.sln add StuffTracker.Api/StuffTracker.Api.csproj 2>/dev/null || echo "  Api already in solution"
dotnet sln StuffTracker.sln add StuffTracker.Tests/StuffTracker.Tests.csproj 2>/dev/null || echo "  Tests already in solution"
echo "✓ Projects added to solution"

# Add reference Api -> Domain (idempotent)
echo "Adding project reference: Api -> Domain..."
if ! grep -q "StuffTracker.Domain.csproj" StuffTracker.Api/StuffTracker.Api.csproj 2>/dev/null; then
    dotnet add StuffTracker.Api/StuffTracker.Api.csproj reference StuffTracker.Domain/StuffTracker.Domain.csproj
    echo "✓ Reference added"
else
    echo "✓ Reference already exists"
fi

# Create base folders
echo "Creating base folders..."
mkdir -p StuffTracker.Domain/Entities
mkdir -p StuffTracker.Api/GraphQL
mkdir -p StuffTracker.Api/Data
mkdir -p StuffTracker.Tests/Integration
echo "✓ Base folders created"

# Build solution
echo "Building solution..."
dotnet build StuffTracker.sln --nologo
echo "✓ Solution builds successfully"

# Initial Git commit
echo "Creating initial Git commit..."
git add .
if ! git diff --cached --quiet; then
    git commit -m "Initialize solution and projects (Task 1.1)"
    echo "✓ Initial commit created"
else
    echo "✓ No changes to commit (already committed)"
fi

echo ""
echo "Task 1.1 initialization complete!"
