using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ScoreCast.Ws.Application.V1.Interfaces;

namespace ScoreCast.Ws.Services;

public sealed class SmtpEmailService(IConfiguration config, ILogger<SmtpEmailService> logger) : IEmailService
{
    public async Task SendWelcomeEmailAsync(string toEmail, string displayName, CancellationToken ct)
    {
        try
        {
            using var client = new SmtpClient("smtp.gmail.com", 587)
            {
                Credentials = new NetworkCredential(
                    config["Email:SmtpUsername"],
                    config["Email:SmtpPassword"]),
                EnableSsl = true
            };

            var from = new MailAddress(
                config["Email:FromAddress"] ?? "nitesh@scorecast.uk",
                config["Email:FromName"] ?? "ScoreCast");

            using var message = new MailMessage(from, new MailAddress(toEmail))
            {
                Subject = $"Welcome to ScoreCast, {displayName}! ⚽",
                IsBodyHtml = true,
                Body = BuildWelcomeHtml(displayName)
            };

            message.ReplyToList.Add(new MailAddress("noreply@scorecast.uk", "ScoreCast (No Reply)"));

            await client.SendMailAsync(message, ct);
            logger.LogInformation("Welcome email sent to {Email}", toEmail);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send welcome email to {Email}", toEmail);
        }
    }

    private static string BuildWelcomeHtml(string displayName) => $"""
        <div style="font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,sans-serif;max-width:600px;margin:0 auto;background:#ffffff;">
            <div style="background:linear-gradient(135deg,#0A1929 0%,#37003C 100%);padding:40px 30px;text-align:center;border-radius:12px 12px 0 0;">
                <div style="font-size:48px;margin-bottom:8px;">⚽</div>
                <h1 style="color:#ffffff;font-size:24px;font-weight:800;margin:0;">Welcome to ScoreCast!</h1>
            </div>

            <div style="padding:30px;color:#333;">
                <p style="font-size:16px;line-height:1.6;">Hey <strong>{displayName}</strong>,</p>

                <p style="font-size:15px;line-height:1.6;">
                    Thanks for joining ScoreCast — the free football predictions app where you compete with friends
                    and the community to prove you know the beautiful game best.
                </p>

                <h3 style="color:#37003C;font-size:16px;margin-top:24px;">Here's what you can do:</h3>

                <table style="width:100%;border-collapse:collapse;margin:16px 0;">
                    <tr>
                        <td style="padding:10px 12px;font-size:20px;vertical-align:top;">🏆</td>
                        <td style="padding:10px 0;font-size:14px;line-height:1.5;">
                            <strong>Predict match scores</strong> — earn points for correct results, goal differences, and exact scorelines.
                            Feeling bold? Use <strong>Risk Plays</strong> to double your points on matches you're confident about
                        </td>
                    </tr>
                    <tr>
                        <td style="padding:10px 12px;font-size:20px;vertical-align:top;">👥</td>
                        <td style="padding:10px 0;font-size:14px;line-height:1.5;">
                            <strong>Create or join prediction leagues</strong> — compete with friends using invite codes
                        </td>
                    </tr>
                    <tr>
                        <td style="padding:10px 12px;font-size:20px;vertical-align:top;">📊</td>
                        <td style="padding:10px 0;font-size:14px;line-height:1.5;">
                            <strong>Live scores &amp; stats</strong> — follow matches in real-time with player stats and league tables
                        </td>
                    </tr>
                    <tr>
                        <td style="padding:10px 12px;font-size:20px;vertical-align:top;">🤖</td>
                        <td style="padding:10px 0;font-size:14px;line-height:1.5;">
                            <strong>AI match insights</strong> — get pre-match analysis to help inform your predictions
                        </td>
                    </tr>
                    <tr>
                        <td style="padding:10px 12px;font-size:20px;vertical-align:top;">🎬</td>
                        <td style="padding:10px 0;font-size:14px;line-height:1.5;">
                            <strong>Highlights reels</strong> — watch goal clips in a TikTok-style feed
                        </td>
                    </tr>
                </table>

                <div style="text-align:center;margin:24px 0;">
                    <a href="https://scorecast.uk/how-to-play" style="display:inline-block;color:#37003C;font-weight:700;font-size:14px;text-decoration:underline;">
                        📖 Learn how scoring works →
                    </a>
                </div>

                <div style="text-align:center;margin:30px 0;">
                    <a href="https://scorecast.uk/dashboard" style="display:inline-block;background:#FF6B35;color:#ffffff;font-weight:700;font-size:16px;padding:14px 32px;border-radius:10px;text-decoration:none;">
                        Start Predicting →
                    </a>
                </div>

                <div style="background:#f0f4ff;border-radius:10px;padding:20px;margin:24px 0;">
                    <h3 style="color:#0A1929;font-size:15px;margin:0 0 8px 0;">📱 Install ScoreCast on your phone</h3>
                    <p style="font-size:13px;line-height:1.5;color:#555;margin:0 0 12px 0;">
                        ScoreCast works as an app on your phone — no app store needed. For the best experience,
                        open the link below in <strong>Google Chrome</strong> and follow the install steps.
                    </p>
                    <a href="https://scorecast.uk/install" style="display:inline-block;background:#0A1929;color:#ffffff;font-weight:600;font-size:13px;padding:10px 20px;border-radius:8px;text-decoration:none;">
                        Install Guide →
                    </a>
                </div>

                <p style="font-size:14px;color:#777;line-height:1.5;">
                    ScoreCast is completely free — no premium tiers, no paywalls. Just football and bragging rights.
                </p>

                <div style="background:#f6f8fa;border-radius:10px;padding:16px 20px;margin:24px 0;border:1px solid #e1e4e8;">
                    <p style="font-size:13px;line-height:1.5;color:#555;margin:0;">
                        🛠️ <strong>Are you a developer?</strong> ScoreCast is open source. Feel free to explore the codebase,
                        open issues, or contribute on
                        <a href="https://github.com/nparajul/ScoreCast" style="color:#37003C;font-weight:600;">GitHub</a>.
                    </p>
                </div>

                <div style="margin-top:28px;padding-top:20px;border-top:1px solid #eee;">
                    <p style="font-size:14px;line-height:1.6;color:#555;margin:0;">
                        Cheers,<br/>
                        <strong style="color:#333;">Nitesh</strong><br/>
                        <span style="font-size:12px;color:#999;">Creator of ScoreCast</span>
                    </p>
                </div>
            </div>

            <div style="background:#f5f7fa;padding:20px 30px;text-align:center;border-radius:0 0 12px 12px;border-top:1px solid #eee;">
                <p style="font-size:12px;color:#999;margin:0;">
                    © 2026 ScoreCast · <a href="https://scorecast.uk" style="color:#37003C;text-decoration:none;">scorecast.uk</a>
                </p>
                <p style="font-size:11px;color:#bbb;margin:8px 0 0 0;">
                    This is an automated message — please do not reply to this email.
                </p>
            </div>
        </div>
        """;
}
