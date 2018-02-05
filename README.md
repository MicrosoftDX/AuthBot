# AuthBot

AuthBot is a .Net library for Azure Active Directory authentication on bots built via Microsoft Bot Framework.

The goals are:

* Support endpoints V1, V2 and B2C (currently only working with v1 and v2, we're still working on the B2C scenario)

* Allow easy and secure sign in, even in chat sessions including multiple users

* Allow to securely sign out, including clearing browser cookies

* Enable scenarios where bots need to communicate with other services such as Office 365 or Azure by obtaining access tokens


## How does it work?

You might want to start by watching this video: https://channel9.msdn.com/Shows/Office-Dev-Show/Office-Dev-Show-Episode-34-Getting-Started-with-Bots-and-the-Microsoft-Graph 

You can run the SampleAADv1Bot locally using Visual Studio and the Microsoft Bot Framework emulator. The sample allows the user to type the following commands:

* logon: triggers the logon flow, which generates a hyperlink. The user clicks at the hyperlink, does the normal OpenID connect flow and at the end a magic number is generated. The user copies the number back into the chat (this guarantees that the bot won't confuse different users if multiple users attempt to click at the same logon link)
* token: demonstrates how to obtain a token once the user is authenticated
* logout: clears state related to the authentication and generates a hyperlink for the user to go and also logout from the web browser


Note: This AuthBot library is also available as a Nuget package here: https://www.nuget.org/packages/AuthBot

## How do I set it up?

A few things you need to know: First, you have to decide the AAD endpoint version you will use:

### AAD endpoint V1:

AAD endpoint V1 is the current, production supported model in Azure AD. This model implies that:

1. You will register an application in Azure Active Directory, using the Azure Management Console https://manage.windowsazure.com
2. During that registration, you will pre-set the permissions your application will need. This means that you won't make those decisions about what types of permissions an app needs in runtime, but instead predefine those when you register your application in Azure AD
3. Every access token will be requested against a resource, not a scope. For example, a resource could be Microsoft Exchange Online and another would be your custom web service
4. Look at the sample V1 and see how it requests tokens for specific resource IDs.


### AAD endpoint V2:

AAD endpoint V2 is a new, more modern way which still has limitations at this point. It enables more flexible scenarios, including AAD B2C which leverages external identity providers such as Facebook and user signup/signin/profile edit support. This model implies that:

1. You will register an application in the new application registration portal under https://apps.dev.microsoft.com, which does not require using Azure Management Portal.
2. You do not specify application permissions during the registration. The application code itself will request for specific permissions in runtime (either during login or any time during the application execution, although this library only allows prompting for consents during logon, so you will need to define the scopes you will need in there when sending the user to the logon page). 
3. If you look at the sample V2 and the OneDrive sample bots, they both use the v2 model.


### AAD endpoint V2 with AAD B2C:

B2C enables users to sign up to your AAD tenant using external identity providers such as Facebook, Google, LinkedIn, Amazon and Microsoft Account. You define policies and the attributes you want to collect during the sign up, for example a user might have to enter their name, e-mail and phone number so that gets recorded into your tenant and accessible to your application.

This model is not yet supported by this library. We're working on it.

### Tips with setup

Regardless of whether you use endpoints v1 or v2, once you register your application you need to set the redirect URI to <your host address>/api/OAuthCallback. For example, if your bot will run on http://localhost:1234, then the redirect URI will have to be http://localhost:1234/api/OAuthCallback. This ensures that once the user finishes the login, your bot will become aware of it.

Then you need to set both this redirect URI, but also the client ID and secret that the web.config. You will also have to say whether you're using endpoints v1 and v2 (also in the web.config) and which permissions (scopes in case of v2 or resource IDs in case of v1) in there. Again, look at the samples and see how they work.

Common mistake: If you register an app in the v1 mode but try to run the OneDrive sample which uses v2, you will get an error. If the bot is configured for v2 you will have to register it using the new v2 portal as discussed above.

In case you are using AAD v1, make sure you are using the ```public AzureAuthDialog(string resourceId, string prompt = "Please click to sign in: ")``` method to initialize the AzureAuthDialog dialog. In AAD v1, the scopes are not supported, they are not needed. And vice versa, if you are using AAD v2, make sure you are using the ```public AzureAuthDialog(string[] scopes, string prompt = "Please click to sign in: ")```. 
 
This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/). For more information, see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.

### Release notes:

3.0.3 - Added signin card support for Skype and custom prompt in the auth dialog constructor

3.1.2 - Updates to use signin card, workaround for the resumption cookie introduced in the latest BotBuilder version, small fixes

3.6.3 - Updated to latest BotBuilder (3.8.1) and the login method has made protected virtual so folks can override it as needed

3.6.5 - Updated to latest BotBuilder (3.13.0.3). Added support for email by removing one of the message, specific explation in email. Added regex to extract the magic number so support in email as well as Teams can work when you have reach text sent or more text (it does happen when you answer email)

