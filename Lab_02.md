# Create a secure ASP.NET MVC 5 web app with log in, email confirmation and password reset (C#)

This lab shows you how to build an ASP.NET MVC 5 web app with email confirmation and password reset using the ASP.NET Identity membership system. 

## Create an ASP.NET MVC app

Start by installing and running Visual Studio Express 2013 for Web or Visual Studio 2013. Install Visual Studio 2013 Update 3 or higher.

1. Create a new ASP.NET Web project and select the MVC template. Web Forms also supports ASP.NET Identity, so you could follow similar steps in a web forms app.

![](img/lab_02/image1.png)

2. Leave the default authentication as Individual User Accounts. If you'd like to host the app in Azure, leave the check box checked. Later in the tutorial we will deploy to Azure. You can open an Azure account for free.

3. Set the project to use SSL.

4. Run the app, click the Register link and register a user. At this point, the only validation on the email is with the [EmailAddress] attribute.

5. In Server Explorer, navigate to Data Connections\DefaultConnection\Tables\AspNetUsers, right click and select Open table definition.

The following image shows the AspNetUsers schema:

![](img/lab_02/image2.png)

6. Right click on the AspNetUsers table and select Show Table Data.

![](img/lab_02/image3.png)

At this point the email has not been confirmed.

7. Click on the row and select delete. You'll add this email again in the next step, and send a confirmation email.

## Email confirmation

It's a best practice to confirm the email of a new user registration to verify they are not impersonating someone else (that is, they haven't registered with someone else's email). Suppose you had a discussion forum, you would want to prevent "bob@example.com" from registering as "joe@contoso.com". Without email confirmation, "joe@contoso.com" could get unwanted email from your app. Suppose Bob accidentally registered as "bib@example.com" and hadn't noticed it, he wouldn't be able to use password recover because the app doesn't have his correct email. Email confirmation provides only limited protection from bots and doesn't provide protection from determined spammers, they have many working email aliases they can use to register.

You generally want to prevent new users from posting any data to your web site before they have been confirmed by email, a SMS text message or another mechanism. In the sections below, we will enable email confirmation and modify the code to prevent newly registered users from logging in until their email has been confirmed.

## Hook up SendGrid

See: https://docs.microsoft.com/en-us/aspnet/core/security/authentication/accconfirm#configure-email-provider 

1. In the Package Manager Console, enter the following command:
```
Install-Package SendGrid
```

2. Go to the Azure SendGrid sign up page and register for a free SendGrid account. Configure SendGrid by adding code similar to the following in App_Start/IdentityConfig.cs:

```cs
public class EmailService : IIdentityMessageService
{
   public async Task SendAsync(IdentityMessage message)
   {
      await configSendGridasync(message);
   }

   // Use NuGet to install SendGrid (Basic C# client lib) 
   private async Task configSendGridasync(IdentityMessage message)
   {
      var myMessage = new SendGridMessage();
      myMessage.AddTo(message.Destination);
      myMessage.From = new System.Net.Mail.MailAddress(
                          "Joe@contoso.com", "Joe S.");
      myMessage.Subject = message.Subject;
      myMessage.Text = message.Body;
      myMessage.Html = message.Body;

      var credentials = new NetworkCredential(
                 ConfigurationManager.AppSettings["mailAccount"],
                 ConfigurationManager.AppSettings["mailPassword"]
                 );

      // Create a Web transport for sending email.
      var transportWeb = new Web(credentials);

      // Send the email.
      if (transportWeb != null)
      {
         await transportWeb.DeliverAsync(myMessage);
      }
      else
      {
         Trace.TraceError("Failed to create Web transport.");
         await Task.FromResult(0);
      }
   }
}
```

You'll need to add the following includes:

```cs
using SendGrid;
using System.Net;
using System.Configuration;
using System.Diagnostics;
```

To keep this sample simple, we'll store the app settings in the web.config file:

```xml
</connectionStrings>
   <appSettings>
      <add key="webpages:Version" value="3.0.0.0" />
      <!-- Markup removed for clarity. -->
      
      <add key="mailAccount" value="xyz" />
      <add key="mailPassword" value="password" />
   </appSettings>
  <system.web>
```

## Enable email confirmation in the Account controller

```cs
//
// POST: /Account/Register
[HttpPost]
[AllowAnonymous]
[ValidateAntiForgeryToken]
public async Task<ActionResult> Register(RegisterViewModel model)
{
    if (ModelState.IsValid)
    {
        var user = new ApplicationUser { UserName = model.Email, Email = model.Email };
        var result = await UserManager.CreateAsync(user, model.Password);
        if (result.Succeeded)
        {
            await SignInManager.SignInAsync(user, isPersistent:false, rememberBrowser:false);

            string code = await UserManager.GenerateEmailConfirmationTokenAsync(user.Id);
            var callbackUrl = Url.Action("ConfirmEmail", "Account", 
               new { userId = user.Id, code = code }, protocol: Request.Url.Scheme);
            await UserManager.SendEmailAsync(user.Id, 
               "Confirm your account", "Please confirm your account by clicking <a href=\"" 
               + callbackUrl + "\">here</a>");

            return RedirectToAction("Index", "Home");
        }
        AddErrors(result);
    }

    // If we got this far, something failed, redisplay form
    return View(model);
}
```

Verify the Views\Account\ConfirmEmail.cshtml file has correct razor syntax. ( The @ character in the first line might be missing. )

```html
@{
    ViewBag.Title = "Confirm Email";
}

<h2>@ViewBag.Title.</h2>
<div>
    <p>
        Thank you for confirming your email. Please @Html.ActionLink("Click here to Log in", "Login", "Account", routeValues: null, htmlAttributes: new { id = "loginLink" })
    </p>
</div>
```

Run the app and click the Register link. Once you submit the registration form, you are logged in.

![](img/lab_02/image4.png)

Check your email account and click on the link to confirm your email.

## Require email confirmation before log in

Currently once a user completes the registration form, they are logged in. You generally want to confirm their email before logging them in. In the section below, we will modify the code to require new users to have a confirmed email before they are logged in (authenticated). Update the HttpPost Register method with the following highlighted changes:

```cs
//
// POST: /Account/Register
[HttpPost]
[AllowAnonymous]
[ValidateAntiForgeryToken]
public async Task<ActionResult> Register(RegisterViewModel model)
{
   if (ModelState.IsValid)
   {
      var user = new ApplicationUser { UserName = model.Email, Email = model.Email };
      var result = await UserManager.CreateAsync(user, model.Password);
      if (result.Succeeded)
      {
         //  Comment the following line to prevent log in until the user is confirmed.
         //  await SignInManager.SignInAsync(user, isPersistent:false, rememberBrowser:false);

         string code = await UserManager.GenerateEmailConfirmationTokenAsync(user.Id);
         var callbackUrl = Url.Action("ConfirmEmail", "Account",
            new { userId = user.Id, code = code }, protocol: Request.Url.Scheme);
         await UserManager.SendEmailAsync(user.Id, "Confirm your account",
            "Please confirm your account by clicking <a href=\"" + callbackUrl + "\">here</a>");

         // Uncomment to debug locally 
         // TempData["ViewBagLink"] = callbackUrl;

         ViewBag.Message = "Check your email and confirm your account, you must be confirmed "
                         + "before you can log in.";

         return View("Info");
         //return RedirectToAction("Index", "Home");
      }
      AddErrors(result);
   }

   // If we got this far, something failed, redisplay form
   return View(model);
}
```

By commenting out the SignInAsync method, the user will not be signed in by the registration. The TempData["ViewBagLink"] = callbackUrl; line can be used to debug the app and test registration without sending email. ViewBag.Message is used to display the confirm instructions. The download sample contains code to test email confirmation without setting up email, and can also be used to debug the application.

Create a Views\Shared\Info.cshtml file and add the following razor markup:

```html
@{
   ViewBag.Title = "Info";
}
<h2>@ViewBag.Title.</h2>
<h3>@ViewBag.Message</h3>
```

Add the Authorize attribute to the Contact action method of the Home controller. You can click on the Contact link to verify anonymous users don't have access and authenticated users do have access.

```cs
[Authorize]
public ActionResult Contact()
{
   ViewBag.Message = "Your contact page.";

   return View();
}
```

You must also update the HttpPost Login action method:

```cs
//
// POST: /Account/Login
[HttpPost]
[AllowAnonymous]
[ValidateAntiForgeryToken]
public async Task<ActionResult> Login(LoginViewModel model, string returnUrl)
{
    if (!ModelState.IsValid)
    {
        return View(model);
    }

    // Require the user to have a confirmed email before they can log on.
    var user = await UserManager.FindByNameAsync(model.Email);
    if (user != null)
    {
       if (!await UserManager.IsEmailConfirmedAsync(user.Id))
       {
          ViewBag.errorMessage = "You must have a confirmed email to log on.";
          return View("Error");
       }
    }

    // This doesn't count login failures towards account lockout
    // To enable password failures to trigger account lockout, change to shouldLockout: true
    var result = await SignInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, shouldLockout: false);
    switch (result)
    {
        case SignInStatus.Success:
            return RedirectToLocal(returnUrl);
        case SignInStatus.LockedOut:
            return View("Lockout");
        case SignInStatus.RequiresVerification:
            return RedirectToAction("SendCode", new { ReturnUrl = returnUrl, RememberMe = model.RememberMe });
        case SignInStatus.Failure:
        default:
            ModelState.AddModelError("", "Invalid login attempt.");
            return View(model);
    }
}
```

Update the Views\Shared\Error.cshtml view to display the error message:

```html
@model System.Web.Mvc.HandleErrorInfo

@{
    ViewBag.Title = "Error";
}

<h1 class="text-danger">Error.</h1>
@{
   if (String.IsNullOrEmpty(ViewBag.errorMessage))
   {
      <h2 class="text-danger">An error occurred while processing your request.</h2>
   }
   else
   {
      <h2 class="text-danger">@ViewBag.errorMessage</h2>
   }
}
```

Delete any accounts in the AspNetUsers table that contain the email alias you wish to test with. Run the app and verify you can't log in until you have confirmed your email address. Once you confirm your email address, click the Contact link.

## Password recovery/reset

Remove the comment characters from the HttpPost ForgotPassword action method in the account controller:

```cs
//
// POST: /Account/ForgotPassword
[HttpPost]
[AllowAnonymous]
[ValidateAntiForgeryToken]
public async Task<ActionResult> ForgotPassword(ForgotPasswordViewModel model)
{
    if (ModelState.IsValid)
    {
        var user = await UserManager.FindByNameAsync(model.Email);
        if (user == null || !(await UserManager.IsEmailConfirmedAsync(user.Id)))
        {
            // Don't reveal that the user does not exist or is not confirmed
            return View("ForgotPasswordConfirmation");
        }

        string code = await UserManager.GeneratePasswordResetTokenAsync(user.Id);
        var callbackUrl = Url.Action("ResetPassword", "Account", new { userId = user.Id, code = code }, protocol: Request.Url.Scheme);
        await UserManager.SendEmailAsync(user.Id, "Reset Password", "Please reset your password by clicking <a href=\"" + callbackUrl + "\">here</a>");
        return RedirectToAction("ForgotPasswordConfirmation", "Account");
    }

    // If we got this far, something failed, redisplay form
    return View(model);
}
```

Remove the comment characters from the ForgotPassword ActionLink in the Views\Account\Login.cshtml razor view file:

```html
@using MvcPWy.Models
@model LoginViewModel
@{
   ViewBag.Title = "Log in";
}

<h2>@ViewBag.Title.</h2>
<div class="row">
   <div class="col-md-8">
      <section id="loginForm">
         @using (Html.BeginForm("Login", "Account", new { ReturnUrl = ViewBag.ReturnUrl }, FormMethod.Post, new { @class = "form-horizontal", role = "form" }))
         {
            @Html.AntiForgeryToken()
            <h4>Use a local account to log in.</h4>
            <hr />
            @Html.ValidationSummary(true, "", new { @class = "text-danger" })
            <div class="form-group">
               @Html.LabelFor(m => m.Email, new { @class = "col-md-2 control-label" })
               <div class="col-md-10">
                  @Html.TextBoxFor(m => m.Email, new { @class = "form-control" })
                  @Html.ValidationMessageFor(m => m.Email, "", new { @class = "text-danger" })
               </div>
            </div>
            <div class="form-group">
               @Html.LabelFor(m => m.Password, new { @class = "col-md-2 control-label" })
               <div class="col-md-10">
                  @Html.PasswordFor(m => m.Password, new { @class = "form-control" })
                  @Html.ValidationMessageFor(m => m.Password, "", new { @class = "text-danger" })
               </div>
            </div>
            <div class="form-group">
               <div class="col-md-offset-2 col-md-10">
                  <div class="checkbox">
                     @Html.CheckBoxFor(m => m.RememberMe)
                     @Html.LabelFor(m => m.RememberMe)
                  </div>
               </div>
            </div>
            <div class="form-group">
               <div class="col-md-offset-2 col-md-10">
                  <input type="submit" value="Log in" class="btn btn-default" />
               </div>
            </div>
            <p>
               @Html.ActionLink("Register as a new user", "Register")
            </p>
            @* Enable this once you have account confirmation enabled for password reset functionality *@
            <p>
               @Html.ActionLink("Forgot your password?", "ForgotPassword")
            </p>
         }
      </section>
   </div>
   <div class="col-md-4">
      <section id="socialLoginForm">
         @Html.Partial("_ExternalLoginsListPartial", new ExternalLoginListViewModel { ReturnUrl = ViewBag.ReturnUrl })
      </section>
   </div>
</div>

@section Scripts {
   @Scripts.Render("~/bundles/jqueryval")
}
```

The Log in page will now show a link to reset the password.

## Resend email confirmation link

Once a user creates a new local account, they are emailed a confirmation link they are required to use before they can log on. If the user accidentally deletes the confirmation email, or the email never arrives, they will need the confirmation link sent again. The following code changes show how to enable this.

Add the following helper method to the bottom of the Controllers\AccountController.cs file:

```cs
private async Task<string> SendEmailConfirmationTokenAsync(string userID, string subject)
{
   string code = await UserManager.GenerateEmailConfirmationTokenAsync(userID);
   var callbackUrl = Url.Action("ConfirmEmail", "Account",
      new { userId = userID, code = code }, protocol: Request.Url.Scheme);
   await UserManager.SendEmailAsync(userID, subject,
      "Please confirm your account by clicking <a href=\"" + callbackUrl + "\">here</a>");

   return callbackUrl;
}
```

Update the Register method to use the new helper:

```cs
//
// POST: /Account/Register
[HttpPost]
[AllowAnonymous]
[ValidateAntiForgeryToken]
public async Task<ActionResult> Register(RegisterViewModel model)
{
   if (ModelState.IsValid)
   {
      var user = new ApplicationUser { UserName = model.Email, Email = model.Email };
      var result = await UserManager.CreateAsync(user, model.Password);
      if (result.Succeeded)
      {
         //  Comment the following line to prevent log in until the user is confirmed.
         //  await SignInManager.SignInAsync(user, isPersistent:false, rememberBrowser:false);

         string callbackUrl = await SendEmailConfirmationTokenAsync(user.Id, "Confirm your account");

         ViewBag.Message = "Check your email and confirm your account, you must be confirmed "
                         + "before you can log in.";

         return View("Info");
         //return RedirectToAction("Index", "Home");
      }
      AddErrors(result);
   }

   // If we got this far, something failed, redisplay form
   return View(model);
}
```

Update the Login method to resend the password if the user account has not been confirmed:

```cs
//
// POST: /Account/Login
[HttpPost]
[AllowAnonymous]
[ValidateAntiForgeryToken]
public async Task<ActionResult> Login(LoginViewModel model, string returnUrl)
{
   if (!ModelState.IsValid)
   {
      return View(model);
   }

   // Require the user to have a confirmed email before they can log on.
  // var user = await UserManager.FindByNameAsync(model.Email);
   var user =  UserManager.Find(model.Email, model.Password);
   if (user != null)
   {
      if (!await UserManager.IsEmailConfirmedAsync(user.Id))
      {
         string callbackUrl = await SendEmailConfirmationTokenAsync(user.Id, "Confirm your account-Resend");

          // Uncomment to debug locally  
          // ViewBag.Link = callbackUrl;
         ViewBag.errorMessage = "You must have a confirmed email to log on. "
                              + "The confirmation token has been resent to your email account.";
         return View("Error");
      }
   }

   // This doesn't count login failures towards account lockout
   // To enable password failures to trigger account lockout, change to shouldLockout: true
   var result = await SignInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, shouldLockout: false);
   switch (result)
   {
      case SignInStatus.Success:
         return RedirectToLocal(returnUrl);
      case SignInStatus.LockedOut:
         return View("Lockout");
      case SignInStatus.RequiresVerification:
         return RedirectToAction("SendCode", new { ReturnUrl = returnUrl, RememberMe = model.RememberMe });
      case SignInStatus.Failure:
      default:
         ModelState.AddModelError("", "Invalid login attempt.");
         return View(model);
   }
}
```

## Combine social and local login accounts

You can combine local and social accounts by clicking on your email link. In the following sequence **RickAndMSFT@gmail.com** is first created as a local login, but you can create the account as a social log in first, then add a local login.

![](img/lab_02/image5.png)

Click on the Manage link. Note the External Logins: 0 associated with this account.

![](img/lab_02/image6.png)

Click the link to another log in service and accept the app requests. The two accounts have been combined, you will be able to log on with either account. You might want your users to add local accounts in case their social log in authentication service is down, or more likely they have lost access to their social account.

In the following image, Tom is a social log in (which you can see from the External Logins: 1 shown on the page).

![](img/lab_02/image7.png)

Clicking on Pick a password allows you to add a local log on associated with the same account.

![](img/lab_02/image8.png)

