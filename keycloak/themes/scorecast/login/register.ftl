<#import "template.ftl" as layout>
<@layout.registrationLayout displayMessage=!messagesPerField.existsError('email','username','password','password-confirm') headerTitle="Create Account" headerSubtitle="Join ScoreCast and start predicting"; section>
    <#if section = "form">
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
