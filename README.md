# Azure DevOps Linkinator

Making [Azure DevOps](https://dev.azure.com) (a.k.a. `AzDO`) links effortlessly short and memorable.

Because [URLs are UI](https://www.hanselman.com/blog/URLsAreUI.aspx), this project provides a nicer UI on top of the default URLs provided by [Azure DevOps](https://dev.azure.com). It does so with the following two components:

* A 100% serverless URL redirection powered by [Azure Functions](http://functions.azure.com) 2.0
* A Google Chrome and [Microsoft Edge Insider](https://www.microsoftedgeinsider.com/) browser extension to seamlessly create and copy the URL to the clipboard from the current [AzDO](https://dev.azure.com) page.

The latter is a must since even the most beautiful URIs are annoying to type by hand :).

## How it works:

1. Install the browser extension from the Chrome store.
2. Navigate to a build, release, work item or wiki page in your [AzDO](https://dev.azure.com) project.
3. Click the AzDO linkinator icon
4. Paste the URL you got on the clipboard and enjoy!

The following are the supported URL shortening schemes:

> NOTE: in all cases, if `org` == `project`, the latter can be omitted.

## Builds

`http://build.azdo.io/{org}/{project}/{id}`

## Releases

`http://releases.azdo.io/{org}/{project}/{id}`

## Work Items

`http://work.azdo.io/{org}/{project}/{id}`

## Wiki

`http://wiki.azdo.io/{org}/{project}/{path}`

## Task Groups

`http://tasks.azdo.io/{org}/{project}`
