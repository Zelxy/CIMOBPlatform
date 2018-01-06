﻿using CIMOBProject.Data;
using Microsoft.EntityFrameworkCore;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Edge;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.Support.UI;
using System;
using System.IO;
using System.Reflection;
using Xunit;

namespace XUnitTesting {
    public class SeleniumTestDummy
    {
        [Fact]
        public void TestWithFirefoxDriver() {
            using (var driver = new FirefoxDriver((Path.GetDirectoryName
                                    (Assembly.GetExecutingAssembly().Location)))) {
                driver.Navigate().GoToUrl
                       (@"https://automatetheplanet.com/multiple-files-page-objects-item-templates/");
                var link = driver.FindElement(By.PartialLinkText("TFS Test API"));
                var jsToBeExecuted = $"window.scroll(0, {link.Location.Y});";
                ((IJavaScriptExecutor)driver).ExecuteScript(jsToBeExecuted);
                var wait = new WebDriverWait(driver, TimeSpan.FromMinutes(1));
                var clickableElement = wait.Until
                         (ExpectedConditions.ElementToBeClickable(By.PartialLinkText("TFS Test API")));
                clickableElement.Click();
            }
        }

        [Fact]
        public void TestWithChromeDriver() {
            using (var driver = new ChromeDriver(Path.GetDirectoryName
                                        (Assembly.GetExecutingAssembly().Location))) {
                driver.Navigate().GoToUrl
                       (@"https://automatetheplanet.com/multiple-files-page-objects-item-templates/");
                var link = driver.FindElement(By.PartialLinkText("TFS Test API"));
                var jsToBeExecuted = $"window.scroll(0, {link.Location.Y});";
                ((IJavaScriptExecutor)driver).ExecuteScript(jsToBeExecuted);
                var wait = new WebDriverWait(driver, TimeSpan.FromMinutes(1));
                var clickableElement = wait.Until
                      (ExpectedConditions.ElementToBeClickable(By.PartialLinkText("TFS Test API")));
                clickableElement.Click();
            }
        }

        [Fact]
        public void TestWithEdgeDriver() {
            using (var driver = new EdgeDriver(Path.GetDirectoryName
                               (Assembly.GetExecutingAssembly().Location))) {
                driver.Navigate().GoToUrl
                     (@"https://automatetheplanet.com/multiple-files-page-objects-item-templates/");
                var link = driver.FindElement(By.PartialLinkText("TFS Test API"));
                var jsToBeExecuted = $"window.scroll(0, {link.Location.Y});";
                ((IJavaScriptExecutor)driver).ExecuteScript(jsToBeExecuted);
                var wait = new WebDriverWait(driver, TimeSpan.FromMinutes(1));
                var clickableElement = wait.Until
                      (ExpectedConditions.ElementToBeClickable(By.PartialLinkText("TFS Test API")));
                clickableElement.Click();
            }
        }
    }
}