# CloudQA Automation Practice Form - Resilient Selenium Tests (C#)

This project contains automated UI tests written in C# using Selenium WebDriver and NUnit. The primary goal is to test specific fields on the CloudQA Automation Practice Form ([https://app.cloudqa.io/home/AutomationPracticeForm](https://app.cloudqa.io/home/AutomationPracticeForm)) using **resilient locators**.

The tests are designed to remain functional even if the position, CSS classes, IDs, or other non-essential HTML properties of the target elements change.

## Features

*   Tests three fields:
    *   **First Name:** Located reliably using its associated `<label>` text ("First Name").
    *   **Mobile Number:** Located using its unique `placeholder` text ("Mobile Number").
    *   **Gender:** Located using its associated `<label>` text ("Gender") and selecting "Female".
*   Employs robust locator strategies (label text, `for`/`id` attributes, relative XPath, placeholders) prioritizing stability over potentially brittle selectors.
*   Uses `WebDriverManager` for automatic ChromeDriver setup.
*   Uses NUnit as the testing framework.
*   Takes screenshots on test failure for easier debugging.

## Technology Stack

*   C# (.NET 6.0 or later recommended)
*   Selenium WebDriver
*   NUnit 3
*   WebDriverManager (for .NET)
*   SeleniumExtras.WaitHelpers

## Setup

1.  **Clone or Download:**
    ```bash
    git clone <your-repository-url>
    cd <repository-directory>
    ```
    Or download the source code ZIP and extract it.

2.  **Restore Dependencies:** Open a terminal or command prompt in the project's root directory (where the `.csproj` file is) and run:
    ```bash
    dotnet restore
    ```
    *(This step might be done automatically by your IDE when opening the project/solution)*

3.  **Build Project:**
    ```bash
    dotnet build
    ```

## Running Tests

There are two main ways to run the tests:

**1. Using Visual Studio Test Explorer:**

*   Open the solution (`.sln`) or project (`.csproj`) in Visual Studio.
*   Build the solution (`Ctrl+Shift+B` or Build > Build Solution).
*   Open the Test Explorer (Test > Test Explorer).
*   The tests (`TestFirstNameInput_ByLabel`, `TestMobileNumberInput_ByLabel`, `TestGenderSelection_ByLabel`) should appear.
*   Right-click on the desired test(s) or the class/project name and select "Run".

**2. Using .NET CLI:**

*   Open a terminal or command prompt in the project's root directory.
*   Run the following command:
    ```bash
    dotnet test
    ```
*   The command will discover, build (if necessary), and execute the tests. A Chrome browser window will launch for each test execution. Results will be displayed in the terminal.

## Configuration

*   **Target URL:** The URL for the practice form (`https://app.cloudqa.io/home/AutomationPracticeForm`) is defined as a constant `FormUrl` within the `AutomationPracticeFormTests.cs` file.
*   **Browser:** Currently configured for Chrome via `WebDriverManager` and `ChromeDriver`.
*   **Wait Timeout:** Explicit waits are set to 30 seconds in the `SetUp` method.

## Resilience Strategy Explained

The key focus is selecting elements without relying on attributes or structures that change frequently:

*   **Labels:** Locating elements via their visible `<label>` text (e.g., "First Name", "Gender") is very robust as this text rarely changes without a functional reason.
*   **`for`/`id` Association:** When using labels, the code prioritizes finding the associated input via the standard HTML `for` (on label) and `id` (on input) attributes, which provides a strong, non-positional link.
*   **Relative XPath:** As a fallback when `for`/`id` is unavailable, relative XPath (e.g., `following-sibling::input[1]`) is used to find elements based on their immediate relationship to the found label. This is more resilient than absolute paths.
*   **Placeholders:** For fields where the placeholder text is unique and descriptive (e.g., "Mobile Number"), using `input[placeholder='...']` provides good resilience against structural and attribute changes (except changes to the placeholder itself).

## Troubleshooting

*   **Test Failures:** If tests fail, check the console output for error messages and stack traces provided by NUnit and Selenium.
*   **Screenshots:** On failure (timeouts or other exceptions), a screenshot is automatically saved to the `bin/Debug/netX.X/Screenshots` directory (relative to the project path). Review the screenshot to see the state of the browser at the time of failure.
*   **Dependencies:** Ensure all prerequisites are installed correctly and run `dotnet restore` if you encounter build issues related to missing packages.
*   **ChromeDriver:** `WebDriverManager` should handle this, but if you have issues, ensure your Chrome browser is up-to-date.
