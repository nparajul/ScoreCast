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
                <div style="color:#ffffff;font-size:24px;font-weight:800;margin:0;">Welcome to ScoreCast!</div>
            </div>

            <div style="padding:30px;color:#333;">
                <p style="font-size:15px;line-height:1.7;">Hey {displayName},</p>

                <p style="font-size:15px;line-height:1.7;">
                    Thanks for signing up! ScoreCast is a free football predictions app where you predict match
                    scores, compete with friends in prediction leagues, and climb the leaderboard.
                </p>

                <p style="font-size:15px;line-height:1.7;">Here's a quick overview of what's available:</p>

                <p style="font-size:14px;line-height:1.8;margin:16px 0;">
                    🏆 <strong>Predict scores</strong> and use <strong>Risk Plays</strong> to double your points<br/>
                    👥 <strong>Prediction leagues</strong> — create one and invite friends with a code<br/>
                    📊 <strong>Live scores</strong>, league tables, and player stats<br/>
                    🤖 <strong>AI match insights</strong> to help with your predictions<br/>
                    🎬 <strong>Goal highlights</strong> in a short-form video feed
                </p>

                <p style="font-size:14px;line-height:1.7;">
                    Check out <a href="https://scorecast.uk/how-to-play" style="color:#37003C;font-weight:600;">How To Play</a>
                    to learn the scoring system, and visit <a href="https://scorecast.uk/install" style="color:#37003C;font-weight:600;">the install guide</a>
                    to add ScoreCast to your phone (works best in Chrome).
                </p>

                <p style="font-size:14px;line-height:1.7;">
                    Head to <a href="https://scorecast.uk/dashboard" style="color:#37003C;font-weight:600;">your dashboard</a> to get started.
                </p>

                <p style="font-size:14px;line-height:1.7;color:#777;">
                    ScoreCast is completely free — no premium tiers, no paywalls. And if you're a developer,
                    the code is open source on <a href="https://github.com/nparajul/ScoreCast" style="color:#37003C;">GitHub</a>.
                </p>

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
