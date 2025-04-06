## **Project Summary: AI-Powered Code Monitoring, Test Updating, and Git Integration**  

### **Overview**  
This project is an **AI-driven code monitoring and test updating system** designed to automate unit test generation and maintenance. It continuously watches for changes in source code files and updates corresponding test files using an AI-powered service. Additionally, it can **monitor changes in a GitHub branch**, automatically generate unit tests, and create a **separate review branch** for validation.  

### **Key Features**  
🔹 **Real-Time Code Monitoring** – Uses a `FileSystemWatcher` to detect changes in `.ext` (configurable) files.  
🔹 **GitHub Branch Monitoring** – Tracks changes in a specified branch and triggers test updates.  
🔹 **Automated Unit Test Updates** – Calls an AI service to update test files when code changes.  
🔹 **Branch Creation for Review** – Creates a new Git branch for updated test files before merging.  
🔹 **Debounce Mechanism** – Prevents excessive processing by batching file events.  
🔹 **Dependency Injection (DI) Support** – Uses DI for `TestUpdater` and `CodeMonitor` for better maintainability.  

### **Core Components**  
1. **ModeRunnerService** (`IModeRunner`)  
   - Initializes the system and starts monitoring the source folder or GitHub branch.  
   - Creates instances of `TestUpdater` and `CodeMonitor` with required dependencies.  

2. **CodeMonitor** (`ICodeMonitor`)  
   - Watches for `.ext` file changes in the source directory.  
   - Uses a debounce mechanism to process file changes efficiently.  
   - Calls `TestUpdater` to update tests when necessary.  

3. **GitMonitor** (`IGitMonitor`)  
   - Periodically checks for new commits in the specified GitHub branch.  
   - Detects modified source files and triggers test updates.  
   - Creates a new Git branch for the updated test files before review.  

4. **TestUpdater** (`ITestUpdater`)  
   - Processes code changes and updates unit tests accordingly.  
   - Interacts with the AI service (`IAIApiService`) to generate test cases.  

5. **AI API Service** (`IAIApiService`)  
   - Provides AI-powered test generation based on modified source code.  

### **How It Works**  
1. **Start Monitoring** – `ModeRunnerService` initializes `CodeMonitor` and/or `GitMonitor` with the source and test folders.  
2. **Detect Changes** – `CodeMonitor` watches for file changes locally, while `GitMonitor` checks for remote updates.  
3. **Debounce Processing** – To prevent unnecessary updates, it waits for a short interval before processing.  
4. **Update Tests** – Calls `TestUpdater`, which uses `IAIApiService` to generate new or modified tests.  
5. **Create Review Branch** – If monitoring Git, a new branch is created with updated test files for review.  

### A step-by-step guide to set up a GitHub personal access token (PAT) with the correct permissions so your app can:

- Create branches
- Get commit history
- Read file content
- Create pull requests

#### STEP-BY-STEP GUIDE: Create and Configure a GitHub Personal Access Token
- Choose Your Token Type

GitHub now offers two types of tokens:
- Classic PAT: Easy to use, global across your account
- Fine-grained PAT: More secure, repository-specific

For simplicity and full access, you can use Classic PAT, unless you need strict security.

#### OPTION 1: Setup a Classic Personal Access Token
1. Go to GitHub Token Settings
👉 https://github.com/settings/tokens

2. Click “Generate new token” → Select “Classic”
3. Set Expiration and Name
E.g. AIUnitTestWriter-PAT

4. Select Scopes:
Make sure to check the following:
   - repo
      - Full control of private repositories
   - workflow (if needed for GitHub Actions)
   - read:org (optional, if your repo is in an organization)

5. Generate Token
Save the token somewhere secure – you won’t see it again!

#### OPTION 2: Setup a Fine-Grained Personal Access Token
More secure, but more config steps

1. Go to:
👉 https://github.com/settings/personal-access-tokens → Generate new token (fine-grained)

2. Repository Access
Select:

Only the repository you want to access (or all repos)

3. Set Repository Permissions
Set the following permissions:

| Category | 	Permission | Level |
| --- | --- | --- |
| Contents	| Read and write	| Required
| Metadata	| Read-only	| Required
| Pull requests | Read and write	| Required
| Issues | 	No access	| (optional)

4. Generate Token
Save the token somewhere secure.

### **Run the Application**
- **Console app** - This mode will prompt for user input and start monitoring the local code files.
- **Background service:** - This mode will automatically start Git branch monitoring and AI-based test generation.

### **Troubleshooting**
- Issue: Git monitoring not working
   - Ensure that the repository URL is correct.
   - Ensure that the API key and model settings are properly configured.
- Issue: AI-generated tests are incomplete
   - Increase MaxTokens value in appsettings.json.
   - Ensure that the AI service endpoint is reachable.

### **Use Case**  
Ideal for teams wanting to automate **unit test maintenance** as their code evolves. The added Git integration makes it perfect for CI/CD pipelines, ensuring that updated test cases are reviewed before merging into the main branch.

### **Contributing**
1. Fork the repository.
2. Create a feature branch (git checkout -b feature/my-new-feature).
3. Commit changes (git commit -am 'Add some feature').
4. Push to the branch (git push origin feature/my-new-feature).
5. Open a Pull Request.