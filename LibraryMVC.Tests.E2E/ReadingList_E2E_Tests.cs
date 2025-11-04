using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using System;
using System.Threading;
using Xunit;

namespace LibraryMVC.Tests.E2E
{
    public class ReadingList_E2E_Tests : IDisposable
    {
        private readonly IWebDriver _driver;
        private readonly string _baseUrl = "https://localhost:44307"; 

        public ReadingList_E2E_Tests()
        {
            _driver = new ChromeDriver();
            _driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(5);
        }

        public void Dispose()
        {
            _driver.Quit();
        }

        [Fact]
        public void Login_And_AddToReadingList_Should_AppearInList()
        {
            
            _driver.Navigate().GoToUrl(_baseUrl + "/Identity/Account/Login");

           
            _driver.FindElement(By.Id("Input_Email")).SendKeys("uliacastuhina@gmail.com");
            _driver.FindElement(By.Id("Input_Password")).SendKeys("Chastuhina13!");
            _driver.FindElement(By.Id("login-submit")).Click();

            
            var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
            wait.Until(d => d.Url == _baseUrl + "/");

            _driver.Navigate().GoToUrl(_baseUrl + "/Books");

            
            var firstBookElement = _driver.FindElement(By.CssSelector("table > tbody > tr"));

            
            var bookTitle = firstBookElement.FindElement(By.CssSelector("td:nth-child(2)")).Text;

           
            var addButton = firstBookElement.FindElement(By.CssSelector(".btn-add-to-list"));
            addButton.Click();

            
            Thread.Sleep(1000);

           

            _driver.Navigate().GoToUrl(_baseUrl + "/ReadingList");

            
            var newlyAddedBookElement = wait.Until(d =>
                d.FindElement(By.XPath($"//*[contains(text(), '{bookTitle}')]"))
            );

            
            Assert.NotNull(newlyAddedBookElement);
        }
    }
}
