<#macro registrationLayout bodyClass="" displayInfo=false displayMessage=true displayRequiredFields=false headerTitle="Welcome" headerSubtitle="">
<!DOCTYPE html>
<html>
<head>
    <meta charset="utf-8">
    <meta name="viewport" content="width=device-width, initial-scale=1">
    <title>${msg("loginTitle",(realm.displayName!'ScoreCast'))}</title>
    <link href="https://fonts.googleapis.com/css?family=Roboto:300,400,500,700&display=swap" rel="stylesheet" />
    <link href="${url.resourcesPath}/css/scorecast.css" rel="stylesheet" />
</head>
<body>
    <div class="sc-page">
        <div class="sc-navbar">
            <a href="http://localhost:5200/" class="sc-navbar-brand">
                <img src="${url.resourcesPath}/img/scorecast-icon.svg" alt="ScoreCast" width="32" height="32" />
                <span>ScoreCast</span>
            </a>
        </div>
        <div class="sc-container">
            <div class="sc-card">
                <div class="sc-card-header">
                    <img src="${url.resourcesPath}/img/scorecast-icon.svg" alt="ScoreCast" width="48" height="48" />
                    <h1>${headerTitle}</h1>
                    <p>${headerSubtitle}</p>
                </div>

                <#if displayMessage && message?has_content && (message.type != 'warning' || !isAppInitiatedAction??)>
                    <div class="sc-alert sc-alert-${message.type}">
                        ${kcSanitize(message.summary)?no_esc}
                    </div>
                </#if>

                <#nested "form">

                <#if displayInfo>
                    <div class="sc-info">
                        <#nested "info">
                    </div>
                </#if>
            </div>
        </div>
    </div>
</body>
</html>
</#macro>
