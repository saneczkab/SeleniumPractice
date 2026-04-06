using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;

namespace SeleniumPractice;

public class StaffTests
{
    private IWebDriver _driver;
    private WebDriverWait _wait;
    private string _baseUrl;
    private string _username;
    private string _password;

    [SetUp]
    public void Setup()
    {
        _driver = new ChromeDriver();
        _wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(5));
    }

    [OneTimeSetUp]
    public void ReadEnv()
    {
        DotNetEnv.Env.Load();
        _baseUrl = Environment.GetEnvironmentVariable("BASE_URL");
        _username = Environment.GetEnvironmentVariable("USERNAME");
        _password = Environment.GetEnvironmentVariable("PASSWORD");
    }

    [TearDown]
    public void TearDown()
    {
        _driver.Quit();
        _driver.Dispose();
    }

    private void Login()
    {
        _driver.Navigate().GoToUrl(_baseUrl);
        _wait.Until(ExpectedConditions.ElementIsVisible(By.Id("Username")));

        var login = _driver.FindElement(By.Id("Username"));
        login.SendKeys(_username);
        var password = _driver.FindElement(By.Id("Password"));
        password.SendKeys(_password);

        var loginButton = _driver.FindElement(By.Name("button"));
        loginButton.Click();

        _wait.Until(ExpectedConditions.UrlToBe($"{_baseUrl}/news"));
    }

    private void OpenSidebar()
    {
        var sidebarButton = _driver.FindElement(By.CssSelector("[data-tid='SidebarMenuButton']"));
        sidebarButton.Click();
        _wait.Until(ExpectedConditions.ElementIsVisible(By.CssSelector("[data-tid='SidePage__root']")));
    }

    private string CreateCommunity(string communityName)
    {
        _driver.Navigate().GoToUrl($"{_baseUrl}/communities");
        var openFormButton = _wait
            .Until(ExpectedConditions.ElementToBeClickable(By.XPath("//button[text()='СОЗДАТЬ']")));
        openFormButton.Click();

        var name = _driver.FindElement(By.CssSelector("[data-tid='Name']"));
        name.SendKeys(communityName);

        var createButton = _driver.FindElement(By.CssSelector("[data-tid='CreateButton']"));
        createButton.Click();

        _wait.Until(ExpectedConditions.UrlContains("settings"));

        return _driver.Url.Replace("settings", "");
    }

    [Test]
    public void AuthorizationTest()
    {
        Login();
        Assert.That(_driver.Title, Does.Contain("Новости"),
            "После успешной авторизации ожидается переход на страницу с заголовком \"Новости\"");
    }

    [Test]
    public void CommunityNavigationTest()
    {
        Login();
        OpenSidebar();

        var communityButton = _driver
            .FindElements(By.CssSelector("[data-tid='Community']"))
            .First(element => element.Displayed);
        communityButton.Click();

        var title = _wait
            .Until(ExpectedConditions.ElementIsVisible(By.CssSelector("[data-tid='Title']")));
        Assert.That(title.Text, Does.Contain("Сообщества"),
            "Ожидается переход на страницу с заголовком \"Сообщества\"");
    }

    [Test]
    public void LogoutTest()
    {
        Login();
        OpenSidebar();

        var logoutButton = _driver.FindElement(By.CssSelector("[data-tid='LogoutButton']"));
        logoutButton.Click();

        var logoutMessage = _wait.Until(ExpectedConditions.ElementIsVisible(
            By.XPath("//h3['Вы вышли из учетной записи']")));

        Assert.That(logoutMessage.Displayed, Is.True,
            "Ожидается отображение сообщения \"Вы вышли из учетной записи\"");
    }

    [Test]
    public void CreateEventRequiredFieldsEmptyTest()
    {
        Login();

        _driver.Navigate().GoToUrl($"{_baseUrl}/events");

        var openFormButton = _wait
            .Until(ExpectedConditions.ElementToBeClickable(By.CssSelector("[data-tid='AddButton']")));
        openFormButton.Click();

        _wait.Until(ExpectedConditions.ElementIsVisible(By.CssSelector("[data-tid='modal-content']")));

        var createButton = _driver
            .FindElement(By.CssSelector("[data-tid='CreateButton']"));
        createButton.Click();

        var validationMessages = _driver
            .FindElements(By.CssSelector("[data-tid='validationMessage']"))
            .Select(message => message.Text.Trim())
            .Where(text => !string.IsNullOrEmpty(text))
            .ToList();
        var validationMessagesDistinct = validationMessages.Distinct().ToList();
        Assert.That(validationMessages, Has.Count.EqualTo(3),
            "Ожидается ровно 3 сообщения об ошибке валидации");
        Assert.That(validationMessagesDistinct, Has.Count.EqualTo(2),
            "Ожидается ровно 2 уникальных сообщения об ошибке валидации");

        Assert.That(validationMessagesDistinct, Does.Contain("Введите корректный ИНН."),
            "Ожидается сообщение об некорректном ИНН");
        Assert.That(validationMessagesDistinct, Does.Contain("Поле обязательно для заполнения."),
            "Ожидается сообщение о том, что поле является обязательным для заполнения");
    }

    [Test]
    public void CommunityTest()
    {
        Login();
        const string communityName = "Selenium test";
        var communityLink = CreateCommunity(communityName);

        var title = _driver.FindElement(By.CssSelector("[data-tid='Title']"));
        Assert.That(title.Text, Does.Contain($"Управление сообществом «{communityName}»"),
            "После создания сообщества ожидается переход на страницу управления им");

        var openDeleteFormButton = _wait.Until(
            ExpectedConditions.ElementToBeClickable(By.CssSelector("[data-tid='DeleteButton']")));
        openDeleteFormButton.Click();
        var deleteFormBody = _driver.FindElement(By.CssSelector("[data-tid='ModalPageBody']"));
        Assert.That(deleteFormBody.Text, 
            Does.Contain($"Вы действительно хотите удалить «{communityName}»?"),
            "Ожидается отображение текст о подтверждении в теле формы удаления сообщества");

        var modalPageFooter = _driver.FindElement(By.CssSelector("[data-tid='ModalPageFooter']"));
        var deleteButton = modalPageFooter
            .FindElement(By.CssSelector("[data-tid='DeleteButton']"));
        deleteButton.Click();
        _wait.Until(ExpectedConditions.TitleContains("Новости"));

        _driver.Navigate().GoToUrl(communityLink);
        var validationMessage = _wait.Until(
            ExpectedConditions.ElementIsVisible(By.CssSelector("[data-tid='ValidationMessage']")));
        Assert.That(validationMessage.Text,
            Does.Contain("Объект не найден. Возможно, он удален или вы переходите по неправильной ссылке"),
            "Ожидается сообщение о том, что сообщество не найдено");
    }
}
