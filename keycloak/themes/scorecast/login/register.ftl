<#import "template.ftl" as layout>
<@layout.registrationLayout displayMessage=!messagesPerField.existsError('email','username','password','password-confirm') headerTitle="Create Account" headerSubtitle="Join ScoreCast and start predicting"; section>
    <#if section = "form">
        <#if social.providers?? && social.providers?size gt 0>
            <#list social.providers as p>
                <a href="${p.loginUrl}" class="sc-social-btn sc-social-${p.alias}">
                    <#if p.alias == "google">
                        <svg viewBox="0 0 24 24" width="20" height="20"><path fill="#4285F4" d="M22.56 12.25c0-.78-.07-1.53-.2-2.25H12v4.26h5.92a5.06 5.06 0 0 1-2.2 3.32v2.77h3.57c2.08-1.92 3.28-4.74 3.28-8.1z"/><path fill="#34A853" d="M12 23c2.97 0 5.46-.98 7.28-2.66l-3.57-2.77c-.98.66-2.23 1.06-3.71 1.06-2.86 0-5.29-1.93-6.16-4.53H2.18v2.84C3.99 20.53 7.7 23 12 23z"/><path fill="#FBBC05" d="M5.84 14.09c-.22-.66-.35-1.36-.35-2.09s.13-1.43.35-2.09V7.07H2.18C1.43 8.55 1 10.22 1 12s.43 3.45 1.18 4.93l2.85-2.22.81-.62z"/><path fill="#EA4335" d="M12 5.38c1.62 0 3.06.56 4.21 1.64l3.15-3.15C17.45 2.09 14.97 1 12 1 7.7 1 3.99 3.47 2.18 7.07l3.66 2.84c.87-2.6 3.3-4.53 6.16-4.53z"/></svg>
                    </#if>
                    Continue with ${p.displayName!"Social"}
                </a>
            </#list>
            <div class="sc-divider">
                <span>or</span>
            </div>
        </#if>
        <form id="kc-register-form" action="${url.registrationAction}" method="post">

            <div class="sc-field">
                <label for="email">Email</label>
                <input id="email" name="email" type="email"
                       value="${(register.formData.email!'')}"
                       class="sc-input" placeholder="Enter your email" autocomplete="email" autofocus />
                <#if messagesPerField.existsError('email')>
                    <span class="sc-error">${kcSanitize(messagesPerField.getFirstError('email'))?no_esc}</span>
                </#if>
            </div>

            <div class="sc-field">
                <label for="username">Username</label>
                <input id="username" name="username" type="text"
                       value="${(register.formData.username!'')}"
                       class="sc-input" placeholder="Choose a username" autocomplete="username" />
                <#if messagesPerField.existsError('username')>
                    <span class="sc-error">${kcSanitize(messagesPerField.getFirstError('username'))?no_esc}</span>
                </#if>
            </div>

            <div class="sc-field">
                <label for="password">Password</label>
                <input id="password" name="password" type="password"
                       class="sc-input" placeholder="Create a password" autocomplete="new-password" />
                <div class="sc-password-requirements">
                    <p>Password must contain:</p>
                    <ul>
                        <li>At least 8 characters</li>
                        <li>1 uppercase letter</li>
                        <li>1 lowercase letter</li>
                        <li>1 number</li>
                        <li>1 special character</li>
                    </ul>
                </div>
                <#if messagesPerField.existsError('password')>
                    <span class="sc-error">${kcSanitize(messagesPerField.getFirstError('password'))?no_esc}</span>
                </#if>
            </div>

            <div class="sc-field">
                <label for="password-confirm">Confirm Password</label>
                <input id="password-confirm" name="password-confirm" type="password"
                       class="sc-input" placeholder="Confirm your password" autocomplete="new-password" />
                <#if messagesPerField.existsError('password-confirm')>
                    <span class="sc-error">${kcSanitize(messagesPerField.getFirstError('password-confirm'))?no_esc}</span>
                </#if>
            </div>

            <input type="hidden" name="firstName" value="" />
            <input type="hidden" name="lastName" value="" />

            <button type="submit" class="sc-btn">Create Account</button>

            <div class="sc-register">
                Already have an account?
                <a href="${url.loginUrl}">Sign in</a>
            </div>
        </form>
    </#if>
</@layout.registrationLayout>
