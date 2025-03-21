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