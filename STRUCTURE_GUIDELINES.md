# ClubSite Project Structure Guidelines

## Core Principle
The ClubSite project maintains a nested directory structure with the main code residing in a `ClubSite/` subdirectory. This structure is intentional and must be respected in all future work.

## Structure Rules
1. **Preserve the nested structure**: The `ClubSite/` subdirectory must remain unchanged
2. **Add new files within the existing structure**: New functionality should be added within the appropriate subdirectories of `ClubSite/`
3. **Follow existing patterns**: Adhere to naming conventions and code organization of the existing project
4. **Do not flatten or restructure**: Never move files from `ClubSite/` to the parent directory
5. **Maintain directory hierarchy**: Keep the existing directory tree (Models/, Services/, Pages/, etc.)

## File Location Examples
- New models: `ClubSite/Models/`
- New services: `ClubSite/Services/`
- New pages: `ClubSite/Pages/`
- New data files: `ClubSite/Data/`

## Important
Any changes to this structure must be explicitly approved. The current nested structure is fundamental to the project's architecture and should be preserved in all future development.
