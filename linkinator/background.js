/// <reference path="../chrome.d.ts" />
function copy(text) {
  var copyFrom = document.createElement('textarea');
  copyFrom.textContent = text;
  var body = document.getElementsByTagName('body')[0];
  body.appendChild(copyFrom);
  copyFrom.select();
  document.execCommand('copy');
  body.removeChild(copyFrom);
}

function onClicked(tab) {
  var parser = document.createElement('a');
  parser.href = tab.url.toString();
  var shortUrl = parser.pathname.substring(1) + parser.search;

  console.info('Processing ' + shortUrl);
  var newUrl = shortenUrl(parser.hostname, shortUrl);
  // If no replacement was made, copy again the original url.
  if (newUrl.skipped)
    newUrl = tab.url;

  copy(newUrl);

  chrome.tabs.executeScript(tab.id, {
      "code": "if (window.jQuery) { $('.workitem-dialog a.caption').attr('href'); } else { '' }"
    }, function (result) {
      href = result[0];
      if (href && href.pathname) {
        var shortUrl = href.pathname.substring(1) + href.search;
        console.info('Processing ' + shortUrl);
        shortUrl = shortenUrl(href.hostname, shortUrl);
        copy(shortUrl);
      }
    });
}

function shortenUrl(hostname, shortUrl) {
  var segments = shortUrl.split('/');
  var org = segments[0];
  var project = segments[1];
  if (hostname == 'devdiv.visualstudio.com')
  {
    // When using subdomains in visualstudio.com, the subdomain is the 
    // actual organization in dev.azure.com/{org}, and the first path 
    // segment is actually the project.
    org = "DevDiv";
    project = org;
  }

  // ================== Wiki ======================
  if (shortUrl.includes('/_wiki/wikis/')) {
    var indexOfPagePath = shortUrl.indexOf('pagePath=') + 9;
    var indexOfEndPath = shortUrl.indexOf('&', indexOfPagePath);
    // Special case DevDiv: we make it even shorter, and switch domains to 
    // place /DevDiv/DevDiv back server-side
    var domain = shortUrl.includes('DevDiv/_wiki/wikis/') ? 'http://wiki.devdiv.io/' : 'http://wiki.azdo.io/';
    var pagePath = indexOfEndPath == -1 ? shortUrl.substring(indexOfPagePath) : shortUrl.substring(indexOfPagePath, indexOfEndPath);
    // The first decode gives us a slash-separated path, which we need to decode individually to decode double encoded chars (i.e. & and -)
    pagePath = decodeURIComponent(pagePath).split('/').map(x => decodeURIComponent(x)).join('/').replace(/^\//, "");
    // page path query string processing hint for azure function:
    // [none] = replace dashes with spaces. Most common case, made short and nice
    // ?u = replace underscores with spaces
    // ?b = bare, do not replace anything (either there were no spaces, which is uncommon in wikis, 
    //      or there were, but there were also both `-` and `_` so no replacement could be provided)
    if (pagePath.indexOf(' ') != -1) {
      // Improve URI when it has spaces
      if (pagePath.indexOf('-') == -1) {
        pagePath = pagePath.replace(/ /g, '-');
      } else if (pagePath.indexOf('_') == -1) {
        pagePath = pagePath.replace(/ /g, '_') + "?u";
      } else {
        // Can't replace spaces, bare treatment to preserve path intact.
        pagePath = pagePath.split('/').map(x => encodeURIComponent(x)).join('/') + "?b";
      }
    } else {
      // No spaces, force bare treatment to preserve path intact.
      pagePath = pagePath.split('/').map(x => encodeURIComponent(x)).join('/') + "?b";
    }

    // This would be the safest way, but it would also URL-encode characters that are 
    // usable in a copy-pasted URL, such as & and - in the URL (browser know how to encode/decode)
    // pagePath = pagePath.split('/').map(x => encodeURIComponent(x)).join('/');

    if (org.toLowerCase() == "devdiv" && project.toLowerCase() == "devdiv")
      return domain + pagePath;
    else 
      return domain + org + '/' + project + '/' + pagePath;
  }

  // We could make wiki pages way shorter by just using the id instead... 
  // if (shortUrl.includes('/_wiki/wikis/DevDiv.wiki/') && shortUrl.includes('pageId=')) {
  //   var pageId = /pageId=(\d+)/.exec(shortUrl);
  //   return 'http://wiki.devdiv.io/' + pageId;
  // }

  // ================== WorkItems ======================
  if (shortUrl.includes('/_workitems/edit/'))
    return 'http://work.azdo.io/' + shortUrl.substring(shortUrl.indexOf('/_workitems/edit/') + 17);

  if (shortUrl.includes('workitem=')) {
    var id = /workitem=(\d+)/.exec(shortUrl);
    return 'http://work.azdo.io/' + id[1];
  }
    
  // ================== Build ======================
  if (shortUrl.includes('/_build')) {
    var buildId = /buildId=(\d+)/.exec(shortUrl);
    var definitionId = /definitionId=(\d+)/.exec(shortUrl);

    var id = buildId ? parseInt(buildId[1]) : parseInt(definitionId[1]);
    var suffix = '';
    if (org.toLowerCase() == "devdiv" && project.toLowerCase() == "devdiv")
      return 'https://build.azdo.io/' + id;
    
    if (org.toLowerCase() != "devdiv") {
      // Match build-azdo function ranges to consider IDs BD or builds.
      if (definitionId && id >= 200)
        suffix = '?d';
      else if (buildId && id < 200)
        suffix = '?b';
    }

    return 'https://build.azdo.io/' + org + '/' + project + '/' + id + suffix;
  }

  if (shortUrl.includes('edit-build-definition&id=')) {
    var buildId = /id=(\d+)/.exec(shortUrl);
    var id = parseInt(buildId[1]);

    if (org.toLowerCase() == "devdiv" && project.toLowerCase() == "devdiv") {
      return 'https://build.azdo.io/' + id;
    } else {
      // Match build-azdo function ranges to consider IDs BD or builds.
      // A >=200 ID will not be considered a BD by default, so force it in that case.
      return 'https://build.azdo.io/' + org + '/' + project + '/' + id + (id >= 200 ? '?d' : '');
    }
  }

  // ================== Release ======================
  if (shortUrl.includes('/_releaseDefinition?definitionId=') || 
      (shortUrl.includes('/_release') && shortUrl.includes('definitionId='))) {
    // New release pipeline
    var definitionId = /definitionId=(\d+)/.exec(shortUrl);
    var id = parseInt(definitionId[1]);

    if (org.toLowerCase() == "devdiv" && project.toLowerCase() == "devdiv") {
      return 'http://release.azdo.io/' + id;
    } else {
      // Match build-azdo function ranges to consider IDs RD or releases.
      // A >=50 ID will not be considered an RD by default, so force it in that case.
      return 'http://release.azdo.io/' + org + '/' + project + '/' + id + (id >= 50 ? '?d' : '');
    }
  }

  if ((shortUrl.includes('/_releaseProgress?') || shortUrl.includes('release-pipeline-progress')) && shortUrl.includes('releaseId=')) {
    // New release pipeline
    var releaseId = /releaseId=(\d+)/.exec(shortUrl);
    var id = parseInt(releaseId[1]);

    if (org.toLowerCase() == "devdiv" && project.toLowerCase() == "devdiv") {
      return 'http://release.azdo.io/' + id;
    } else {
      // Match build-azdo function ranges to consider IDs RD or releases.
      // A <50 ID will not be considered a release by default (an RD instead), so force it in that case.
      return 'http://release.azdo.io/' + org + '/' + project + '/' + id + (id < 50 ? '?r' : '');
    }
  }

  // ================== PullRequest ======================
  if (org.toLowerCase() == "devdiv" && project.toLowerCase() == "devdiv" && shortUrl.includes('/pullrequest/')) {
    var match = /_git\/(.+)\/pullrequest\/(\d+)/.exec(shortUrl);
    if (match[1] == 'VS') 
      // Make the default project VS, to make it even shorter
      return 'http://pr.devdiv.io/' + match[2];
    else
      return 'http://pr.devdiv.io/' + match[1] + '/' + match[2];
  }

  if (shortUrl.includes('content/problem/')) {
    var problemId = /problem\/(\d+)\//.exec(shortUrl);
    return 'http://feedback.devdiv.io/' + problemId[1];
  }
  
  return { skipped: true };
}

chrome.webNavigation.onCommitted.addListener(function(e) {
  chrome.tabs.query( { active: true, currentWindow: true }, function( tabs ) {
    var tab = tabs[0];
    var url = tab.url.replace("devdiv.visualstudio.com/", "dev.azure.com/DevDiv/");
    chrome.tabs.update(tabs[0].id, { url: url } ); 
  });
}, { url: [{ hostSuffix: 'devdiv.visualstudio.com' }]});

chrome.pageAction.onClicked.addListener(onClicked);

// When the extension is installed or upgraded ...
chrome.runtime.onInstalled.addListener(function() {
  // Replace all rules ...
  chrome.declarativeContent.onPageChanged.removeRules(undefined, function() {
    // With a new rule ...
    chrome.declarativeContent.onPageChanged.addRules([
      {
        // That fires when a page's URL contains 'devdiv.visualstudio.com' ...
        conditions: [
          new chrome.declarativeContent.PageStateMatcher({
            pageUrl: { urlContains: 'devdiv.visualstudio.com' },
          }),
          new chrome.declarativeContent.PageStateMatcher({
            pageUrl: { urlContains: 'dev.azure.com' },
          }),
          new chrome.declarativeContent.PageStateMatcher({
            pageUrl: { urlContains: 'developercommunity.visualstudio.com/content/problem/' },
          })
        ],
        // And shows the extension's page action.
        actions: [ new chrome.declarativeContent.ShowPageAction() ]
      }
    ]);
  });
});