## **Project Summary: AI-Powered Code Monitoring and Test Updating**  

### **Overview**  
This project is an **AI-driven code monitoring and test updating system** designed to automate unit test generation and maintenance. It continuously watches for changes in source code files and updates corresponding test files using an AI-powered service.  

### **Key Features**  
🔹 **Real-Time Code Monitoring** – Uses a `FileSystemWatcher` to detect changes in `.cs` files.  
🔹 **Automated Unit Test Updates** – Calls an AI service to update test files when code changes.  
🔹 **Debounce Mechanism** – Prevents excessive processing by batching file events.  
🔹 **Dependency Injection (DI) Support** – Uses DI for `TestUpdater` and `CodeMonitor` for better maintainability.  

### **Core Components**  
1. **ModeRunnerService** (`IModeRunner`)  
   - Initializes the system and starts monitoring the source folder.  
   - Creates instances of `TestUpdater` and `CodeMonitor` with required dependencies.  

2. **CodeMonitor** (`ICodeMonitor`)  
   - Watches for `.cs` file changes in the source directory.  
   - Uses a debounce mechanism to process file changes efficiently.  
   - Calls `TestUpdater` to update tests when necessary.  

3. **TestUpdater** (`ITestUpdater`)  
   - Processes code changes and updates unit tests accordingly.  
   - Interacts with the AI service (`IAIApiService`) to generate test cases.  

4. **AI API Service** (`IAIApiService`)  
   - Provides AI-powered test generation based on modified source code.  

### **How It Works**  
1. **Start Monitoring** – `ModeRunnerService` initializes `CodeMonitor` with the source and test folders.  
2. **Detect Changes** – `CodeMonitor` watches for changes in `.cs` files.  
3. **Debounce Processing** – To prevent unnecessary updates, it waits for a short interval before processing.  
4. **Update Tests** – Calls `TestUpdater`, which uses `IAIApiService` to generate new or modified tests.  

### **Use Case**  
Ideal for teams practicing **Test-Driven Development (TDD)** or wanting to automate **unit test maintenance** as their code evolves. This system reduces manual effort and ensures **consistent test coverage** across a project. 🚀