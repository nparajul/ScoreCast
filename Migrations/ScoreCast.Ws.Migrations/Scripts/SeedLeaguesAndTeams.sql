-- Seed: Country, Competitions, Season, Teams (with football-data.org external IDs), SeasonTeam

INSERT INTO scorecast.country (name, code, external_id, flag_url, is_active, created_by, created_by_app)
VALUES ('England', 'ENG', '2072', 'https://crests.football-data.org/770.svg', true, 'seed', 'ScoreCast')
ON CONFLICT DO NOTHING;

INSERT INTO scorecast.competition (name, code, country_id, logo_url, external_id, type, sort_order, is_active, created_by, created_by_app)
SELECT 'Premier League', 'PL', c.id, 'https://crests.football-data.org/PL.png', '2021', 'League', 1, true, 'seed', 'ScoreCast'
FROM scorecast.country c WHERE c.name = 'England'
ON CONFLICT DO NOTHING;

INSERT INTO scorecast.competition (name, code, country_id, external_id, type, sort_order, is_active, created_by, created_by_app)
SELECT 'Championship', 'ELC', c.id, '2016', 'League', 2, true, 'seed', 'ScoreCast'
FROM scorecast.country c WHERE c.name = 'England'
ON CONFLICT DO NOTHING;

INSERT INTO scorecast.season (name, competition_id, external_id, start_date, end_date, current_matchday, is_current, created_by, created_by_app)
SELECT '2025/26', comp.id, '733', '2025-08-16', '2026-05-24', 37, true, 'seed', 'ScoreCast'
FROM scorecast.competition comp WHERE comp.name = 'Premier League'
ON CONFLICT DO NOTHING;

INSERT INTO scorecast.team (name, short_name, external_id, country_id, founded, venue, club_colors, website, is_active, created_by, created_by_app)
SELECT t.name, t.short_name, t.ext_id, c.id, t.founded, t.venue, t.colors, t.website, true, 'seed', 'ScoreCast'
FROM (VALUES
  ('Arsenal FC', 'ARS', '57', 1886, 'Emirates Stadium', 'Red / White', 'http://www.arsenal.com'),
  ('Aston Villa FC', 'AVL', '58', 1874, 'Villa Park', 'Claret / Sky Blue', 'http://www.avfc.co.uk'),
  ('AFC Bournemouth', 'BOU', '1044', 1899, 'Vitality Stadium', 'Red / Black', 'http://www.afcb.co.uk'),
  ('Brentford FC', 'BRE', '402', 1889, 'Gtech Community Stadium', 'Red / White', 'http://www.brentfordfc.com'),
  ('Brighton & Hove Albion FC', 'BHA', '397', 1901, 'American Express Stadium', 'Blue / White', 'http://www.seagulls.co.uk'),
  ('Chelsea FC', 'CHE', '61', 1905, 'Stamford Bridge', 'Royal Blue / White', 'http://www.chelseafc.com'),
  ('Crystal Palace FC', 'CRY', '354', 1905, 'Selhurst Park', 'Red / Blue', 'http://www.cpfc.co.uk'),
  ('Everton FC', 'EVE', '62', 1878, 'Goodison Park', 'Blue / White', 'http://www.evertonfc.com'),
  ('Fulham FC', 'FUL', '63', 1879, 'Craven Cottage', 'White / Black', 'http://www.fulhamfc.com'),
  ('Ipswich Town FC', 'IPS', '349', 1878, 'Portman Road', 'Blue / White', 'http://www.itfc.co.uk'),
  ('Leicester City FC', 'LEI', '338', 1884, 'King Power Stadium', 'Royal Blue / White', 'http://www.lcfc.com'),
  ('Liverpool FC', 'LIV', '64', 1892, 'Anfield', 'Red / White', 'http://www.liverpoolfc.tv'),
  ('Manchester City FC', 'MCI', '65', 1880, 'Etihad Stadium', 'Sky Blue / White', 'https://www.mancity.com'),
  ('Manchester United FC', 'MUN', '66', 1878, 'Old Trafford', 'Red / White', 'http://www.manutd.com'),
  ('Newcastle United FC', 'NEW', '67', 1892, 'St. James'' Park', 'Black / White', 'http://www.nufc.co.uk'),
  ('Nottingham Forest FC', 'NFO', '351', 1865, 'The City Ground', 'Red / White', 'http://www.nottinghamforest.co.uk'),
  ('Southampton FC', 'SOU', '340', 1885, 'St. Mary''s Stadium', 'Red / White', 'http://www.saintsfc.co.uk'),
  ('Tottenham Hotspur FC', 'TOT', '73', 1882, 'Tottenham Hotspur Stadium', 'White / Navy Blue', 'http://www.tottenhamhotspur.com'),
  ('West Ham United FC', 'WHU', '563', 1895, 'London Stadium', 'Claret / Blue', 'http://www.whufc.com'),
  ('Wolverhampton Wanderers FC', 'WOL', '76', 1877, 'Molineux Stadium', 'Old Gold / Black', 'http://www.wolves.co.uk')
) AS t(name, short_name, ext_id, founded, venue, colors, website)
CROSS JOIN scorecast.country c WHERE c.name = 'England'
ON CONFLICT DO NOTHING;

INSERT INTO scorecast.season_team (season_id, team_id, created_by, created_by_app)
SELECT s.id, t.id, 'seed', 'ScoreCast'
FROM scorecast.season s
CROSS JOIN scorecast.team t
WHERE s.name = '2025/26'
  AND s.competition_id = (SELECT id FROM scorecast.competition WHERE name = 'Premier League')
ON CONFLICT DO NOTHING;
