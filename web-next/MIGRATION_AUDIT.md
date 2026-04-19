# ScoreCast Next.js Migration — Feature Audit

## Legend
- 🔴 **Critical gap** — core functionality missing
- 🟡 **Partial** — has structure but missing details/sub-features
- 🟢 **Good** — close to feature parity
- ⚪ **Simple page** — minimal Blazor, port is adequate

---

## Priority 1 — Core User Flow (fix first)

### 🔴 Predict Page (`/predict/[seasonId]`)
**Blazor 740 lines → Next.js 133 lines (18%)**

Missing from Blazor:
- **Risk Plays section** — Clean Sheet Bet, First Goal Team, Over/Under Goals (400+ lines of logic)
- Points breakdown panel with pending bonus, grand total, max possible
- Scoring rules drawer (how points work)
- Expand/collapse risk plays section
- Match selection for risk plays (which match you're targeting)
- Validation via FluentValidation on predict submission
- Auto-scroll to first unpredicted match
- Deadline countdown timer
- Warning when predictions locked
- "Make Predictions" success feedback
- AppName audit field tracking

### 🔴 Match Detail Page (`/matches/[id]`)
**Blazor 1259 lines → Next.js 391 lines (31%)**

Missing:
- Full events timeline with all event types (goals/cards/subs) and icons
- Community predictions heatmap
- Your prediction vs community average
- Head-to-head matches section
- Form guide (last 5 results)
- Player stats for match
- AI insights card with probabilities
- Live minute indicator with pulse animation
- Second yellow card handling (stacked icon)
- Own goal special styling
- Substitution pairing (in/out on same row)
- Match insight AI commentary
- Prediction deadline status

### 🔴 Dashboard Page (`/dashboard`)
**Blazor 524 lines → Next.js 162 lines (31%)**

Missing:
- Full onboarding check + carousel trigger for new users
- Welcome banner for email-unverified users
- Last match replay card (fully functional, not placeholder)
- Best prediction highlight
- Streak tracking display with fire emoji
- Gameweek comparison visualization (your vs community)
- Enrollment flow for new seasons (+ button should open season picker)
- Create/join league buttons
- League invite code display

---

## Priority 2 — Data Pages

### 🟡 Team Detail (`/teams/[id]`)
**Blazor 515 lines → Next.js 118 lines (23%)**

Missing:
- Competition history tabs
- Next match countdown card
- Form guide W/D/L badges
- Complete squad grouping with coach card
- Results/fixtures tabs with competition filter
- Player stats for team
- Head coach info
- Team colors, founded year, venue details

### 🟡 Competition Detail (`/competitions/[id]`)
**Blazor 490 lines → Next.js 234 lines (48%)**

Missing:
- Season dropdown working with state persistence
- Zone legend (UCL/Europa/Relegation)
- Scores tab: full gameweek nav with 30s polling
- Players tab: mobile pill tabs + desktop sortable table full logic
- Highlight user's favorite team in table

### 🟡 Scores Page (`/scores`)
**Blazor 447 lines → Next.js 123 lines (28%)**

Missing:
- Live match polling with in-place score updates
- Sticky header with live pulse chip
- Match tile expansion showing events
- Your predictions alongside scores
- Competition filter state persistence
- Scroll restoration between navigations

### 🟡 Player Profile (`/dashboard/[leagueId]/player/[userId]`)
**Blazor 434 lines → Next.js 118 lines (27%)**

Missing:
- Matches list with their predictions per gameweek
- Points breakdown panel (how they scored points)
- Risk plays display (if visible)
- Scoring rules with expand/collapse
- Prediction visibility gating (before kickoff)

### 🟡 Player Stats (`/player-stats`)
**Blazor 371 lines → Next.js 128 lines (35%)**

Missing:
- Filter by team
- Clean sheets tracking
- Position filter (GK/DEF/MID/FWD)
- Goals per game / assists per game calculations
- Minutes played
- Sticky header offset from app bar

### 🟡 Master Data Sync (`/data-sync`)
**Blazor 514 lines → Next.js 116 lines (23%)**

Missing:
- Per-competition status badges (synced/pending/error)
- Progress display for batch operations
- Sync result history
- Individual operation buttons with loading states
- "Generate Insights" button
- Calculate matchday button

---

## Priority 3 — Global / Community

### 🟡 Home / Landing (`/`)
**Blazor 302 lines → Next.js 164 lines (54%)**

Missing:
- ScoreCast logo SVG (using emoji placeholder)
- "Powered By" section (Pulse API, Football-Data, FPL, OpenAI, Firebase)
- All feature cards match Blazor content and colors

### 🟡 Global Dashboard (`/global/*`)
**Blazor 291 lines → Next.js 57 lines spread across 5 pages**

Missing:
- Gameweek deadline countdown timer
- Community progress bar (% of users who've predicted)
- Recap best predictor podium
- Stats visualization (hardest match, most predictable team)
- Prediction distribution pie/bar charts

### 🟡 Settings (`/settings`)
**Blazor 236 lines → Next.js 154 lines (65%)**

Missing:
- Profanity filter on display name change
- Username change with availability check
- Avatar upload/change
- Dark mode toggle? (memory bank says forced light)
- Email notification preferences
- Account deletion flow

### 🟡 Insights (`/insights`)
**Blazor 250 lines → Next.js 125 lines (50%)**

Missing:
- Insight details expand/collapse
- Key stats breakdown (form, H2H, xG chart)
- Markdown rendering for AI summary

---

## Priority 4 — Auth & Smaller

### 🟡 Login (`/login`)
**Blazor 166 lines → Next.js 91 lines (55%)**

Missing:
- Email verification check after login (redirect to /verify-email if not verified)
- UserSync trigger (done via auth-context, need to verify)
- "Remember me" checkbox
- Error message i18n matching Firebase error codes

### 🟡 Register (`/register`)
**Blazor 166 lines → Next.js 77 lines (46%)**

Missing:
- Display name profanity check on submit
- Password strength indicator
- Terms of service checkbox

### 🟡 Verify Email (`/verify-email`)
**Blazor 118 lines → Next.js 72 lines (61%)**

Missing:
- Auto-check every 3 seconds
- Resend button with proper cooldown
- Sign out option
- Auto-redirect when verified

### 🟡 Prediction Replay (`/replay/[matchId]/[userId]`)
**Blazor 233 lines → Next.js 170 lines (73%)**

Missing:
- OG share meta tags
- Share as image (SVG)
- "Join ScoreCast" CTA for non-logged-in visitors
- Context-aware share text

### 🟢 Gameweek Replay
**Blazor 153 → Next.js 109 (71%)** — mostly OK

### 🟢 Settings, How To Play, Install — reasonable

### ⚪ Simple pages (adequate as-is)
- `/logout` (simple redirect)
- `/not-found`
- `/global/page` (redirect)

---

## Priority 5 — Missing entirely in Next.js

### 🔴 Onboarding Carousel — **NOT PORTED**
- 6-step carousel: Welcome → Predict&Score → Leagues → Install → Display Name → Fav Team
- Profanity validation on display name
- Triggered on new user signup
- `HasCompletedOnboarding` flag on user profile

### 🔴 Welcome Dialog — **NOT PORTED**
- Alternative simpler welcome flow

### 🔴 PageGuard — **NOT PORTED**
- Redirects unauthenticated users to /login
- Currently every page manually checks auth

### 🔴 PWA Install Banner — **NOT PORTED**
- Component shown at bottom when install prompt available
- Dismissible

### 🔴 Alert/Snackbar System — **NOT PORTED**
- Success/error toasts
- Replaces Blazor IAlertService/MudSnackbar

### 🔴 Loading Service — **NOT PORTED**
- Branded pulsing ScoreCast icon loader
- Global loading overlay

### 🔴 State Preservation Service — **NOT PORTED**
- Tab state per page (CompetitionDetail, TeamDetail, MatchPage)
- Scroll position restoration

### 🔴 Role Navigation Service — **NOT PORTED**
- Dynamic nav based on user role
- Currently hardcoded menu items

### 🔴 Firebase Token Refresh — **NOT PORTED**
- Auto-refresh token before expiry
- Handle 401 retry

---

## Components Not Ported

| Blazor Component | Status |
|---|---|
| MatchTile | 🟡 Basic version exists |
| CompetitionFilter | 🟢 Works |
| PwaInstallBanner | 🔴 Missing |
| UserSync | 🟢 Ported into auth-context |
| PageGuard | 🔴 Missing |
| WelcomeDialog | 🔴 Missing |
| OnboardingCarousel | 🔴 Missing |
| ScoreCastLoading | 🔴 Missing (using emoji) |
| AlertHost | 🔴 Missing |

---

## Recommended Order of Attack

1. **Predict page** — users spend most time here (risk plays, points breakdown, deadline)
2. **Dashboard** — landing page after login, needs to feel complete
3. **Match page** — second most visited (events, lineups, H2H, community)
4. **Onboarding carousel** — blocks new user experience
5. **PageGuard** — proper auth redirect instead of manual checks
6. **Alert/toast system** — for proper feedback
7. **Scores page** — live polling, match tile expansion
8. **Competition detail** — scores/players tabs
9. **Team detail** — results, fixtures, squad tabs
10. **Everything else** — per usage

---

## Estimated Effort

- **Predict page full port**: 3-4 hours (risk plays UI alone is ~2h)
- **Match page full port**: 4-5 hours
- **Dashboard**: 2-3 hours
- **Onboarding**: 2-3 hours
- **Scores**: 2 hours
- **Other pages**: 1-2 hours each
- **Infrastructure** (PageGuard, Alerts, Loading): 2-3 hours

**Total: 30-40 hours of focused work** to reach feature parity with Blazor.
