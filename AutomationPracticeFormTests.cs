using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using System;
using System.IO; // Needed for Path
using WebDriverManager; // Using WebDriverManager nuget package
using WebDriverManager.DriverConfigs.Impl; // Using WebDriverManager nuget package
using SeleniumExtras.WaitHelpers; // For ExpectedConditions
using System.Threading;

namespace CloudQAResilientTest
{
    [TestFixture]
    public class AutomationPracticeFormTests
    {
        private IWebDriver? _driver;
        private WebDriverWait? _wait;
        private const string FormUrl = "https://app.cloudqa.io/home/AutomationPracticeForm";
        private string _screenshotDirectory = Path.Combine(TestContext.CurrentContext.WorkDirectory, "Screenshots");

        [SetUp]
        public void SetupTest()
        {
            Directory.CreateDirectory(_screenshotDirectory);
            new DriverManager().SetUpDriver(new ChromeConfig());
            ChromeOptions options = new ChromeOptions();
            options.AddArgument("--disable-extensions");
            options.AddArgument("--disable-popup-blocking");
            options.AddArgument("--start-maximized");
            // options.AddArgument("--headless");
            _driver = new ChromeDriver(options);

            if (_driver == null) { Assert.Fail("WebDriver (_driver) was not initialized."); return; }
            _wait = new WebDriverWait(_driver!, TimeSpan.FromSeconds(30));

            try {
                 _driver.Navigate().GoToUrl(FormUrl);
                 _wait!.Until(drv => ((IJavaScriptExecutor)_driver).ExecuteScript("return document.readyState").Equals("complete"));
                 Console.WriteLine("Page ready state is complete.");
            } catch (Exception ex) {
                 TakeScreenshot("Setup_PageLoadError");
                 Assert.Fail($"An error occurred during page load or ready state check: {ex.Message}");
            }
        }

        // *** MODIFIED TO USE LABEL FOR INCREASED RELIABILITY ***
        [Test(Description = "Test First Name input using its Label text")]
        public void TestFirstNameInput_ByLabel() // Renamed for clarity
        {
            string firstNameToEnter = "Tester";
            string labelText = "First Name"; // The unique text of the label to find
            string testName = TestContext.CurrentContext.Test.Name;

            // 1. Locator for the Label based on its text
            By labelLocator = By.XPath($"//label[normalize-space(text())='{labelText}']");

            IWebElement? firstNameInput = null; // Variable to hold the found input

            try
            {
                if (_wait == null) Assert.Fail("WebDriverWait (_wait) was not initialized in Setup.");

                Console.WriteLine($"{testName}: Waiting for label exists: {labelLocator}");
                // Find the label element first
                IWebElement nameLabel = _wait.Until(ExpectedConditions.ElementIsVisible(labelLocator));
                Console.WriteLine($"{testName}: Label '{labelText}' is visible.");

                ScrollIntoView(nameLabel); // Scroll label into view

                // 2. Find Associated Input (Attempt 1: Using label's 'for' attribute and input's 'id')
                string? inputId = nameLabel.GetAttribute("for");
                if (!string.IsNullOrEmpty(inputId))
                {
                    try
                    {
                        By inputLocatorById = By.Id(inputId);
                        Console.WriteLine($"{testName}: Label has 'for' attribute ('{inputId}'). Waiting for input by ID: {inputLocatorById}");
                        // Wait for the input linked by ID to be visible
                        firstNameInput = _wait.Until(ExpectedConditions.ElementIsVisible(inputLocatorById));
                        Console.WriteLine($"{testName}: Found input using label 'for' attribute and input ID.");
                    }
                    catch (WebDriverTimeoutException ex) { Console.WriteLine($"{testName}: Timeout finding input by ID '{inputId}'. Will try relative lookup. Error: {ex.Message}"); }
                    catch (NoSuchElementException ex) { Console.WriteLine($"{testName}: Input not found immediately by ID '{inputId}'. Will try relative lookup. Error: {ex.Message}"); }
                }
                else { Console.WriteLine($"{testName}: Label does not have a 'for' attribute. Trying relative lookup."); }

                // 2. Find Associated Input (Attempt 2: Relative XPath Fallback - check input following the label)
                if (firstNameInput == null)
                {
                    try
                    {
                        // Looks for the first <input> tag that is the next sibling of the label
                        By relativeInputLocator = By.XPath("./following-sibling::input[1]");
                        Console.WriteLine($"{testName}: Attempting to find input via relative XPath: {relativeInputLocator}");
                        // Find element relative to the already found 'nameLabel'
                        firstNameInput = nameLabel.FindElement(relativeInputLocator);
                        // Check visibility explicitly after finding via relative path
                        if (!firstNameInput.Displayed) {
                             Console.WriteLine($"{testName}: Input found via relative XPath but is not displayed. Waiting for visibility.");
                             // Need to wait on the specific element reference
                              WebDriverWait visibilityWait = new WebDriverWait(_driver!, TimeSpan.FromSeconds(10)); // Shorter wait for visibility check
                              visibilityWait.Until(drv => firstNameInput.Displayed);
                        }
                         Console.WriteLine($"{testName}: Found and confirmed visible input using relative XPath.");
                    }
                    catch (NoSuchElementException ex) {
                         // Add one more fallback: Sometimes the input is inside a sibling div
                         try {
                             By relativeInputInDivLocator = By.XPath("./following-sibling::div/input[1]");
                             Console.WriteLine($"{testName}: Relative sibling input not found. Trying input inside sibling div: {relativeInputInDivLocator}");
                             firstNameInput = nameLabel.FindElement(relativeInputInDivLocator);
                               if (!firstNameInput.Displayed) {
                                 Console.WriteLine($"{testName}: Input found via relative div/XPath but is not displayed. Waiting for visibility.");
                                 WebDriverWait visibilityWait = new WebDriverWait(_driver!, TimeSpan.FromSeconds(10));
                                 visibilityWait.Until(drv => firstNameInput.Displayed);
                               }
                               Console.WriteLine($"{testName}: Found and confirmed visible input using relative div/XPath.");
                         }
                         catch (NoSuchElementException) {
                             Assert.Fail($"{testName}: Could not find the input associated with label '{labelText}' using 'for'/ID or relative XPath (following-sibling or following-sibling/div/input). Error: {ex.Message}");
                         }
                    }
                }

                // Ensure input was found by one of the methods
                Assert.IsNotNull(firstNameInput, $"Failed to find input element associated with label '{labelText}'.");

                // 3. Interact and Verify with the found input element
                Console.WriteLine($"{testName}: Interacting with the identified input field.");
                firstNameInput.Clear();
                firstNameInput.SendKeys(firstNameToEnter);
                Assert.AreEqual(firstNameToEnter, firstNameInput.GetAttribute("value"), "First Name value mismatch.");
                Console.WriteLine($"{testName}: PASSED");

            }
            catch (WebDriverTimeoutException ex) {
                 TakeScreenshot($"{testName}_Timeout");
                 Assert.Fail($"Timed out during test execution, potentially while waiting for label '{labelLocator}' or associated input. See screenshot. Error: {ex.Message}");
            }
            catch (Exception ex) {
                TakeScreenshot($"{testName}_Error");
                if (ex is AssertionException) throw;
                Assert.Fail($"An unexpected error occurred during {testName}: {ex.GetType().Name} - {ex.Message}");
            }
        }


        [Test(Description = "Test Mobile Number input using its Label text")]
        public void TestMobileNumberInput_ByLabel()
        {
            string mobileNumberToEnter = "1234567890";
            string labelText = "Mobile #"; // The label text from the image
            string testName = TestContext.CurrentContext.Test.Name;

            // 1. Locator for the Label based on its text
            By labelLocator = By.XPath($"//label[normalize-space(text())='{labelText}']");

            IWebElement? mobileNumberInput = null; // Variable to hold the found input

            try
            {
                if (_wait == null) Assert.Fail("WebDriverWait (_wait) was not initialized in Setup.");

                Console.WriteLine($"{testName}: Waiting for label exists: {labelLocator}");
                // Find the label element first
                IWebElement mobileLabel = _wait.Until(ExpectedConditions.ElementIsVisible(labelLocator));
                Console.WriteLine($"{testName}: Label '{labelText}' is visible.");

                ScrollIntoView(mobileLabel); // Scroll label into view

                // 2. Find Associated Input (Attempt 1: Using label's 'for' attribute and input's 'id')
                string? inputId = mobileLabel.GetAttribute("for");
                if (!string.IsNullOrEmpty(inputId))
                {
                    try
                    {
                        By inputLocatorById = By.Id(inputId);
                        Console.WriteLine($"{testName}: Label has 'for' attribute ('{inputId}'). Waiting for input by ID: {inputLocatorById}");
                        // Wait for the input linked by ID to be visible
                        mobileNumberInput = _wait.Until(ExpectedConditions.ElementIsVisible(inputLocatorById));
                        Console.WriteLine($"{testName}: Found input using label 'for' attribute and input ID.");
                    }
                    catch (WebDriverTimeoutException ex) { 
                        Console.WriteLine($"{testName}: Timeout finding input by ID '{inputId}'. Will try relative lookup. Error: {ex.Message}"); 
                    }
                    catch (NoSuchElementException ex) { 
                        Console.WriteLine($"{testName}: Input not found immediately by ID '{inputId}'. Will try relative lookup. Error: {ex.Message}"); 
                    }
                }
                else { 
                    Console.WriteLine($"{testName}: Label does not have a 'for' attribute. Trying relative lookup."); 
                }

                // 2. Find Associated Input (Attempt 2: Relative XPath Fallback - check input following the label)
                if (mobileNumberInput == null)
                {
                    try
                    {
                        // Looks for the first <input> tag that is the next sibling of the label
                        By relativeInputLocator = By.XPath("./following-sibling::input[1]");
                        Console.WriteLine($"{testName}: Attempting to find input via relative XPath: {relativeInputLocator}");
                        // Find element relative to the already found 'mobileLabel'
                        mobileNumberInput = mobileLabel.FindElement(relativeInputLocator);
                        // Check visibility explicitly after finding via relative path
                        if (!mobileNumberInput.Displayed) {
                            Console.WriteLine($"{testName}: Input found via relative XPath but is not displayed. Waiting for visibility.");
                            // Need to wait on the specific element reference
                            WebDriverWait visibilityWait = new WebDriverWait(_driver!, TimeSpan.FromSeconds(10)); // Shorter wait for visibility check
                            visibilityWait.Until(drv => mobileNumberInput.Displayed);
                        }
                        Console.WriteLine($"{testName}: Found and confirmed visible input using relative XPath.");
                    }
                    catch (NoSuchElementException ex) {
                        // Add one more fallback: Sometimes the input is inside a sibling div
                        try {
                            By relativeInputInDivLocator = By.XPath("./following-sibling::div/input[1]");
                            Console.WriteLine($"{testName}: Relative sibling input not found. Trying input inside sibling div: {relativeInputInDivLocator}");
                            mobileNumberInput = mobileLabel.FindElement(relativeInputInDivLocator);
                            if (!mobileNumberInput.Displayed) {
                                Console.WriteLine($"{testName}: Input found via relative div/XPath but is not displayed. Waiting for visibility.");
                                WebDriverWait visibilityWait = new WebDriverWait(_driver!, TimeSpan.FromSeconds(10));
                                visibilityWait.Until(drv => mobileNumberInput.Displayed);
                            }
                            Console.WriteLine($"{testName}: Found and confirmed visible input using relative div/XPath.");
                        }
                        catch (NoSuchElementException) {
                            // Final fallback: Try to find by placeholder
                            try {
                                By placeholderLocator = By.CssSelector("input[placeholder='Mobile Number']");
                                Console.WriteLine($"{testName}: Previous methods failed. Attempting to find by placeholder: {placeholderLocator}");
                                mobileNumberInput = _wait.Until(ExpectedConditions.ElementIsVisible(placeholderLocator));
                                Console.WriteLine($"{testName}: Found input using placeholder attribute.");
                            }
                            catch (Exception placeholderEx) {
                                // One more attempt: Try to find any input near the label
                                try {
                                    By nearbyInputLocator = By.XPath($"//label[contains(text(), 'Mobile')]/following::input[1]");
                                    Console.WriteLine($"{testName}: Trying broader XPath to find any input near Mobile label: {nearbyInputLocator}");
                                    mobileNumberInput = _wait.Until(ExpectedConditions.ElementExists(nearbyInputLocator));
                                    Console.WriteLine($"{testName}: Found input using broader XPath near Mobile label.");
                                }
                                catch (Exception finalEx) {
                                    Assert.Fail($"{testName}: Could not find the input associated with label '{labelText}' using any method. Error: {ex.Message}, Placeholder error: {placeholderEx.Message}, Final error: {finalEx.Message}");
                                }
                            }
                        }
                    }
                }

                // Ensure input was found by one of the methods
                Assert.IsNotNull(mobileNumberInput, $"Failed to find input element associated with label '{labelText}'.");

                // 3. Interact and Verify with the found input element
                Console.WriteLine($"{testName}: Interacting with the identified input field.");
                mobileNumberInput.Clear();
                mobileNumberInput.SendKeys(mobileNumberToEnter);
                Assert.AreEqual(mobileNumberToEnter, mobileNumberInput.GetAttribute("value"), "Mobile Number value mismatch.");
                Console.WriteLine($"{testName}: PASSED - Mobile number successfully entered");
            }
            catch (WebDriverTimeoutException ex) {
                TakeScreenshot($"{testName}_Timeout");
                Assert.Fail($"Timed out during test execution, potentially while waiting for label '{labelLocator}' or associated input. See screenshot. Error: {ex.Message}");
            }
            catch (Exception ex) {
                TakeScreenshot($"{testName}_Error");
                if (ex is AssertionException) throw;
                Assert.Fail($"An unexpected error occurred during {testName}: {ex.GetType().Name} - {ex.Message}");
            }
        }

        [Test(Description = "Test Gender selection using its Label text")]
        public void TestGenderSelection_ByLabel()
        {
            string genderToSelect = "Female"; // We'll select the "Female" option
            string labelText = "Gender"; // The unique text of the main label
            string testName = TestContext.CurrentContext.Test.Name;

            // 1. Locator for the Gender label based on its text
            By labelLocator = By.XPath($"//label[normalize-space(text())='{labelText}']");

            try
            {
                if (_wait == null) Assert.Fail("WebDriverWait (_wait) was not initialized in Setup.");

                Console.WriteLine($"{testName}: Waiting for '{labelText}' label to exist: {labelLocator}");
                // Find the main Gender label element first
                IWebElement genderLabel = _wait.Until(ExpectedConditions.ElementIsVisible(labelLocator));
                Console.WriteLine($"{testName}: Label '{labelText}' is visible.");

                ScrollIntoView(genderLabel); // Scroll label into view

                // 2. Find the specific gender radio option (in this case "Female")
                // First try to find it by the label text associated with the radio button
                By genderOptionLocator = By.XPath($"//label[normalize-space(text())='{genderToSelect}']");
                
                IWebElement? genderOptionLabel = null;
                IWebElement? radioButton = null;
                
                // Try to find the specific gender option label
                try 
                {
                    Console.WriteLine($"{testName}: Looking for gender option label '{genderToSelect}'");
                    genderOptionLabel = _wait.Until(ExpectedConditions.ElementIsVisible(genderOptionLocator));
                    Console.WriteLine($"{testName}: Found gender option label '{genderToSelect}'");
                }
                catch (WebDriverTimeoutException ex)
                {
                    Console.WriteLine($"{testName}: Could not find label for '{genderToSelect}' using direct XPath. Trying alternative approach. Error: {ex.Message}");
                    // Alternative: Look for radio buttons near the main gender label
                    genderOptionLabel = null;
                }

                // If we found the gender option label, try to find the associated radio button
                if (genderOptionLabel != null)
                {
                    // First attempt: Check if the label has a "for" attribute pointing to the radio button
                    string? radioId = genderOptionLabel.GetAttribute("for");
                    if (!string.IsNullOrEmpty(radioId))
                    {
                        try
                        {
                            By radioLocatorById = By.Id(radioId);
                            Console.WriteLine($"{testName}: Label has 'for' attribute ('{radioId}'). Looking for radio by ID");
                            radioButton = _wait.Until(ExpectedConditions.ElementExists(radioLocatorById));
                            Console.WriteLine($"{testName}: Found radio button using label 'for' attribute and input ID");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"{testName}: Could not find radio button by ID '{radioId}'. Error: {ex.Message}");
                        }
                    }
                    
                    // Second attempt: Look for a radio button that is a preceding sibling of the label
                    if (radioButton == null)
                    {
                        try
                        {
                            By radioPrecedingSibling = By.XPath("./preceding-sibling::input[@type='radio'][1]");
                            Console.WriteLine($"{testName}: Looking for radio button as preceding sibling of label");
                            radioButton = genderOptionLabel.FindElement(radioPrecedingSibling);
                            Console.WriteLine($"{testName}: Found radio button as preceding sibling of label");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"{testName}: Could not find radio as preceding sibling. Error: {ex.Message}");
                        }
                    }
                    
                    // Third attempt: Look for a radio button that is a child of the label
                    if (radioButton == null)
                    {
                        try
                        {
                            By radioChild = By.XPath(".//input[@type='radio']");
                            Console.WriteLine($"{testName}: Looking for radio button as child of label");
                            radioButton = genderOptionLabel.FindElement(radioChild);
                            Console.WriteLine($"{testName}: Found radio button as child of label");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"{testName}: Could not find radio as child of label. Error: {ex.Message}");
                        }
                    }
                }

                // If we still haven't found the radio button, try a more general approach
                if (radioButton == null)
                {
                    // Look for all radio buttons and find the one next to the text we want
                    Console.WriteLine($"{testName}: Trying to find radio button by proximity to text '{genderToSelect}'");
                    By allRadiosLocator = By.XPath("//input[@type='radio']");
                    var allRadios = _driver.FindElements(allRadiosLocator);
                    
                    foreach (var radio in allRadios)
                    {
                        // Get parent element to check for text
                        IWebElement parent = (IWebElement)((IJavaScriptExecutor)_driver).ExecuteScript("return arguments[0].parentNode;", radio);
                        string parentText = parent.Text.Trim();
                        
                        // Check if this parent contains our gender text
                        if (parentText.Contains(genderToSelect))
                        {
                            radioButton = radio;
                            Console.WriteLine($"{testName}: Found radio button with parent text containing '{genderToSelect}'");
                            break;
                        }
                        
                        // Also check next sibling for text (in case label is after the radio)
                        IWebElement nextSibling = null;
                        try
                        {
                            nextSibling = radio.FindElement(By.XPath("./following-sibling::*[1]"));
                            string siblingText = nextSibling.Text.Trim();
                            if (siblingText.Contains(genderToSelect))
                            {
                                radioButton = radio;
                                Console.WriteLine($"{testName}: Found radio button with next sibling text containing '{genderToSelect}'");
                                break;
                            }
                        }
                        catch
                        {
                            // No next sibling or other issue, continue to next radio
                        }
                    }
                }

                // Final attempt: If all else fails, try a direct XPath that looks for a radio near the text
                if (radioButton == null)
                {
                    try
                    {
                        By directRadioLocator = By.XPath($"//input[@type='radio'][../text()[contains(.,'{genderToSelect}')] or ./following-sibling::text()[contains(.,'{genderToSelect}')] or ./following-sibling::*[text()[contains(.,'{genderToSelect}')]] or ./preceding-sibling::*[text()[contains(.,'{genderToSelect}')]]]");
                        Console.WriteLine($"{testName}: Attempting direct XPath for radio near text '{genderToSelect}'");
                        radioButton = _wait.Until(ExpectedConditions.ElementExists(directRadioLocator));
                        Console.WriteLine($"{testName}: Found radio button using direct XPath");
                    }
                    catch (Exception ex)
                    {
                        TakeScreenshot($"{testName}_DirectXPathFailed");
                        Assert.Fail($"Could not find radio button for gender '{genderToSelect}' using any method. Error: {ex.Message}");
                    }
                }

                // Ensure radio button was found by one of the methods
                Assert.IsNotNull(radioButton, $"Failed to find radio button for gender '{genderToSelect}'");

                // 3. Interact with the found radio button
                Console.WriteLine($"{testName}: Interacting with the identified radio button");
                ScrollIntoView(radioButton);
                
                // Try standard click first
                try
                {
                    // Check if already selected to handle idempotency
                    if (!radioButton.Selected)
                    {
                        radioButton.Click();
                        Console.WriteLine($"{testName}: Standard click on radio button");
                    }
                    else
                    {
                        Console.WriteLine($"{testName}: Radio button was already selected");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"{testName}: Standard click failed: {ex.Message}. Trying JavaScript click.");
                    // Try JavaScript click as fallback
                    if (_driver is IJavaScriptExecutor jsExec)
                    {
                        jsExec.ExecuteScript("arguments[0].click();", radioButton);
                        Console.WriteLine($"{testName}: JavaScript click on radio button");
                    }
                    else
                    {
                        throw new InvalidOperationException("Cannot perform JavaScript click, driver does not support IJavaScriptExecutor");
                    }
                }
                
                // Verify the radio button is selected
                Assert.IsTrue(radioButton.Selected, $"Radio button for gender '{genderToSelect}' was not successfully selected.");
                Console.WriteLine($"{testName}: PASSED - Gender '{genderToSelect}' was successfully selected");
            }
            catch (WebDriverTimeoutException ex)
            {
                TakeScreenshot($"{testName}_Timeout");
                Assert.Fail($"Timed out during test execution, potentially while waiting for gender label '{labelLocator}' or associated radio button. See screenshot. Error: {ex.Message}");
            }
            catch (Exception ex)
            {
                TakeScreenshot($"{testName}_Error");
                if (ex is AssertionException) throw;
                Assert.Fail($"An unexpected error occurred during {testName}: {ex.GetType().Name} - {ex.Message}");
            }
        }

        [TearDown]
        public void CleanupTest()
        {
            _driver?.Quit();
        }

        // --- Helper Methods ---

        private void ScrollIntoView(IWebElement element)
        {
            try {
                if (_driver is IJavaScriptExecutor jsExec) {
                    jsExec.ExecuteScript("arguments[0].scrollIntoView({block: 'center'});", element);
                    System.Threading.Thread.Sleep(200);
                } else { Console.WriteLine("Warning: Driver is null or does not support JavaScript, cannot scroll into view."); }
            } catch (Exception ex) { Console.WriteLine($"Warning: Failed to scroll element into view. {ex.GetType().Name} - {ex.Message}"); }
        }

        private void TakeScreenshot(string stepName)
        {
            if (_driver == null) { Console.WriteLine("Skipping screenshot: WebDriver is null."); return; }
            ITakesScreenshot? screenshotDriver = _driver as ITakesScreenshot;
             if (screenshotDriver == null) { Console.WriteLine("Skipping screenshot: Driver does not support ITakesScreenshot."); return; }
            try {
                Screenshot screenshot = screenshotDriver.GetScreenshot();
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string invalidChars = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
                string safeStepName = stepName;
                foreach (char c in invalidChars) { safeStepName = safeStepName.Replace(c.ToString(), "_"); }
                string fileName = $"{safeStepName}_{timestamp}.png";
                string filePath = Path.Combine(_screenshotDirectory, fileName);
                screenshot.SaveAsFile(filePath);
                Console.WriteLine($"Screenshot saved: {filePath}");
                TestContext.AddTestAttachment(filePath, $"Screenshot for step: {stepName}");
            } catch (Exception ex) { Console.WriteLine($"Failed to take or save screenshot: {ex.Message}"); }
        }
    }
}