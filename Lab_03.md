# ASP.NET MVC 5 app with SMS and email Two-Factor Authentication

## Create an ASP.NET MVC app

Start by installing and running Visual Studio Express 2013 for Web or Visual Studio 2013. Install Visual Studio 2013 Update 3 or higher.

1. Create a new ASP.NET Web project and select the MVC template. Web Forms also supports ASP.NET Identity, so you could follow similar steps in a web forms app.

![](img/lab_03/image1.png)

2. Leave the default authentication as Individual User Accounts. If you'd like to host the app in Azure, leave the check box checked. Later in the tutorial we will deploy to Azure. You can open an Azure account for free.
3. Set the project to use SSL.

## Set up SMS for Two-factor authentication

1. Creating a User Account with an SMS provider

Create a Twilio(https://www.twilio.com/try-twilio) or an ASPSMS(https://www.aspsms.com/asp.net/identity/testcredits/) account.

2. Installing additional packages or adding service references

Twilio:
In the Package Manager Console, enter the following command:
```
Install-Package Twilio
```
ASPSMS:
The following service reference needs to be added:

![](img/lab_03/image2.png)

Address:
```
https://webservice.aspsms.com/aspsmsx2.asmx?WSDL
```

Namespace:
```
ASPSMSX2
```

3. Figuring out SMS Provider User credentials

Twilio:
From the Dashboard tab of your Twilio account, copy the Account SID and Auth token.

ASPSMS:
From your account settings, navigate to Userkey and copy it together with your self-defined Password.

We will later store these values in the web.config file within the keys "SMSAccountIdentification" and "SMSAccountPassword" .

4. Specifying SenderID / Originator

Twilio:
From the Numbers tab, copy your Twilio phone number.

ASPSMS:
Within the Unlock Originators Menu, unlock one or more Originators or choose an alphanumeric Originator (Not supported by all networks).

We will later store this value in the web.config file within the key "SMSAccountFrom"

5. Transferring SMS provider credentials into app

Make the credentials and sender phone number available to the app. To keep things simple we will store these values in the web.config file. When we deploy to Azure, we can store the values securely in the app settings section on the web site configure tab.

```xml
</connectionStrings>
   <appSettings>
      <add key="webpages:Version" value="3.0.0.0" />
      <!-- Markup removed for clarity. -->
      <!-- SendGrid-->
      <add key="mailAccount" value="account" />
      <add key="mailPassword" value="password" />
      <add key="SMSAccountIdentification" value="My Identification" />
      <add key="SMSAccountPassword" value="My Password" />
      <add key="SMSAccountFrom" value="+12065551234" />
   </appSettings>
  <system.web>
```

6. Implementation of data transfer to SMS provider

Configure the SmsService class in the App_Start\IdentityConfig.cs file.

Depending on the used SMS provider activate either the Twilio or the ASPSMS section:

```cs
public class SmsService : IIdentityMessageService
{
    public Task SendAsync(IdentityMessage message)
    {
        // Twilio Begin
        //var accountSid = ConfigurationManager.AppSettings["SMSAccountIdentification"];
        //var authToken = ConfigurationManager.AppSettings["SMSAccountPassword"];
        //var fromNumber = ConfigurationManager.AppSettings["SMSAccountFrom"];

        //TwilioClient.Init(accountSid, authToken);

        //MessageResource result = MessageResource.Create(
            //new PhoneNumber(message.Destination),
            //from: new PhoneNumber(fromNumber),
           //body: message.Body
        //);

        ////Status is one of Queued, Sending, Sent, Failed or null if the number is not valid
         //Trace.TraceInformation(result.Status.ToString());
        ////Twilio doesn't currently have an async API, so return success.
         //return Task.FromResult(0);    
        // Twilio End

        // ASPSMS Begin 
        // var soapSms = new MvcPWx.ASPSMSX2.ASPSMSX2SoapClient("ASPSMSX2Soap");
        // soapSms.SendSimpleTextSMS(
        //   System.Configuration.ConfigurationManager.AppSettings["SMSAccountIdentification"],
        //   System.Configuration.ConfigurationManager.AppSettings["SMSAccountPassword"],
        //   message.Destination,
        //   System.Configuration.ConfigurationManager.AppSettings["SMSAccountFrom"],
        //   message.Body);
        // soapSms.Close();
        // return Task.FromResult(0);
        // ASPSMS End
    }
}
```

7. Update the Views\Manage\Index.cshtml Razor view: (note: don't just remove the comments in the exiting code, use the code below.)

```html
@model MvcPWy.Models.IndexViewModel
@{
   ViewBag.Title = "Manage";
}
<h2>@ViewBag.Title.</h2>
<p class="text-success">@ViewBag.StatusMessage</p>
<div>
   <h4>Change your account settings</h4>
   <hr />
   <dl class="dl-horizontal">
      <dt>Password:</dt>
      <dd>
         [
         @if (Model.HasPassword)
         {
            @Html.ActionLink("Change your password", "ChangePassword")
         }
         else
         {
            @Html.ActionLink("Create", "SetPassword")
         }
         ]
      </dd>
      <dt>External Logins:</dt>
      <dd>
         @Model.Logins.Count [
         @Html.ActionLink("Manage", "ManageLogins") ]
      </dd>
        <dt>Phone Number:</dt>
      <dd>
         @(Model.PhoneNumber ?? "None") [
         @if (Model.PhoneNumber != null)
         {
            @Html.ActionLink("Change", "AddPhoneNumber")
            @: &nbsp;|&nbsp;
            @Html.ActionLink("Remove", "RemovePhoneNumber")
         }
         else
         {
            @Html.ActionLink("Add", "AddPhoneNumber")
         }
         ]
      </dd>
      <dt>Two-Factor Authentication:</dt> 
      <dd>
         @if (Model.TwoFactor)
         {
            using (Html.BeginForm("DisableTwoFactorAuthentication", "Manage", FormMethod.Post, new { @class = "form-horizontal", role = "form" }))
            {
               @Html.AntiForgeryToken()
               <text>Enabled
                  <input type="submit" value="Disable" class="btn btn-link" />
               </text>
            }
         }
         else
         {
            using (Html.BeginForm("EnableTwoFactorAuthentication", "Manage", FormMethod.Post, new { @class = "form-horizontal", role = "form" }))
            {
               @Html.AntiForgeryToken()
               <text>Disabled
                  <input type="submit" value="Enable" class="btn btn-link" />
               </text>
            }
         }
      </dd>
   </dl>
</div>
```

8. Verify the EnableTwoFactorAuthentication and DisableTwoFactorAuthentication action methods in the ManageController have the[ValidateAntiForgeryToken] attribute:

```cs
//
// POST: /Manage/EnableTwoFactorAuthentication
[HttpPost,ValidateAntiForgeryToken]
public async Task<ActionResult> EnableTwoFactorAuthentication()
{
    await UserManager.SetTwoFactorEnabledAsync(User.Identity.GetUserId(), true);
    var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
    if (user != null)
    {
        await SignInAsync(user, isPersistent: false);
    }
    return RedirectToAction("Index", "Manage");
}
//
// POST: /Manage/DisableTwoFactorAuthentication
[HttpPost, ValidateAntiForgeryToken]
public async Task<ActionResult> DisableTwoFactorAuthentication()
{
    await UserManager.SetTwoFactorEnabledAsync(User.Identity.GetUserId(), false);
    var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
    if (user != null)
    {
        await SignInAsync(user, isPersistent: false);
    }
    return RedirectToAction("Index", "Manage");
}
```

9. Run the app and log in with the account you previously registered.

10. Click on your User ID, which activates the Index action method in Manage controller.

![](img/lab_03/image3.png)

11. Click Add.

![](img/lab_03/image4.png)

12. The AddPhoneNumber action method displays a dialog box to enter a phone number that can receive SMS messages.

```cs
// GET: /Account/AddPhoneNumber
public ActionResult AddPhoneNumber()
{
   return View();
}
```

![](img/lab_03/image5.png)

13. In a few seconds you will get a text message with the verification code. Enter it and press Submit.

![](img/lab_03/image6.png)

14. The Manage view shows your phone number was added.

## Enable two-factor authentication

In the template generated app, you need to use the UI to enable two-factor authentication (2FA). To enable 2FA, click on your user ID (email alias) in the navigation bar.

![](img/lab_03/image7.png)

Click on enable 2FA.

![](img/lab_03/image8.png)

Logout, then log back in.

![](img/lab_03/image9.png)

The Verify Code page is displayed where you can enter the code (from SMS or email).

![](img/lab_03/image10.png)

Clicking on the Remember this browser check box will exempt you from needing to use 2FA to log in when using the browser and device where you checked the box. As long as malicious users can't gain access to your device, enabling 2FA and clicking on the Remember this browser will provide you with convenient one step password access, while still retaining strong 2FA protection for all access from non-trusted devices. You can do this on any private device you regularly use.
