# .NET SDK Setup

This project requires .NET 8.0 SDK. If you encounter SDK version errors, follow these steps:

## Quick Fix

In your current terminal session, run:
```bash
export DOTNET_ROOT="$HOME/.dotnet"
export PATH="$DOTNET_ROOT:$PATH"
```

Then verify:
```bash
dotnet --list-sdks
```

You should see `8.0.415` listed.

## Permanent Fix

The `~/.zshrc` file has been updated to include DOTNET_ROOT. To apply it:

**Option 1:** Restart your terminal, or  
**Option 2:** Run `source ~/.zshrc`

## Verify Installation

After updating your shell:
```bash
cd StuffTracker.Api
dotnet run
```

The project should now run successfully.

## Troubleshooting

If you still see errors:
1. Check that .NET 8.0 is installed: `dotnet --list-sdks`
2. Verify `global.json` exists in the project root (it pins the SDK version)
3. Ensure you're running commands from the project directory where `global.json` is located

