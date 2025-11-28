<#import "template.ftl" as layout>
<@layout.registrationLayout displayMessage=!messagesPerField.existsError('username','password') headerTitle="Welcome Back" headerSubtitle="Sign in to your ScoreCast account"; section>
    <#if section = "form">
        <form id="kc-form-login" action="${url.loginAction}" method="post">
            <div class="sc-field">
                <label for="username">${msg("usernameOrEmail")}</label>
                <input id="username" name="username" type="text" autofocus autocomplete="username"
                       value="${(login.username!'')}"
                       class="sc-input" placeholder="Enter your username or email" />
                <#if messagesPerField.existsError('username')>
                    <span class="sc-error">${kcSanitize(messagesPerField.getFirstError('username'))?no_esc}</span>
                </#if>
            </div>

            <div class="sc-field">
                <label for="password">${msg("password")}</label>
                <input id="password" name="password" type="password" autocomplete="current-password"
                       class="sc-input" placeholder="Enter your password" />
                <#if messagesPerField.existsError('password')>
                    <span class="sc-error">${kcSanitize(messagesPerField.getFirstError('password'))?no_esc}</span>
                </#if>
            </div>

            <#if realm.rememberMe && !usernameEditDisabled??>
                <div class="sc-remember">
                    <label>
                        <input id="rememberMe" name="rememberMe" type="checkbox"
                               <#if login.rememberMe??>checked</#if>> Remember me
                    </label>
                </div>
            </#if>

            <button type="submit" class="sc-btn">Sign In</button>

            <#if realm.resetPasswordAllowed>
                <div class="sc-links">
                    <a href="${url.loginResetCredentialsUrl}">Forgot password?</a>
                </div>
            </#if>

            <#if realm.registrationAllowed>
                <div class="sc-register">
                    Don't have an account?
                    <a href="${url.registrationUrl}">Sign up</a>
                </div>
            </#if>
        </form>
    </#if>
</@layout.registrationLayout>
