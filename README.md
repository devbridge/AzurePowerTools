DevBridge Azure Power Tools (http://www.devbridge.com)
=======================================================================
Our Azure Power Tools project is the collection of Windows Azure related tools and extensions. Here at DevBridge, we believe in open communication and like to share our knowledge with others who may find it helpful. 

You can also take a look at our [Standard Web Project Template](https://github.com/devbridge/StandardWebProjectTemplate).

## Basic authentication for Windows Azure websites
Basic authentication for Windows Azure websites is a HTTP managed module that provides basic authentication for web applications hosted in Windows Azure websites. For more information please read this [blog post](http://www.devbridge.com/articles/basic-authentication-for-windows-azure-websites).

Basic authentication module has relation to two projects:
- Devbridge.BasicAuthentication project has the implementation for the basic authentication module.
- Devbridge.BasicAuthentication.Test is simple test website that can be used to test basic authentication.

###Configuration Settings
* `allowRedirects`: indicates whether redirects are allowed without authentication.
* `excludes`: allows to configure rules to exclude certain parts of application from authentication.

####Sample excludes
```
<!-- exclude POST requests to URLs starting with /home; other requests (GET to /home/index, POST to /account/login) should be authenticated -->
<add url="^/home(.*)" verb="post" />
<!-- exclude POST requests to all URLs; other requests (GET to /home/index, DELETE to /account/123) should be authenticated -->
<add url="" verb="post" />
<!-- exclude all requests to URLs starting with /allow; other requests should be authenticated -->
<add url="^/allow(.*)" verb="" />
<!-- exclude all requests to URLs starting with /home; rules specified below overwrite previous rules with the same url pattern.  -->
<add url="^/home(.*)" verb="" />
<!-- exclude all requests to URLs on the production site; other requests should be authenticated -->
<add url="^www.my-production-site.com" verb="" />
```
