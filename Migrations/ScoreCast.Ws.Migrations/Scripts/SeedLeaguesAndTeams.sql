-- Seed: Country, Premier League, and 2025/26 teams

INSERT INTO scorecast.country_master (name, code, is_active, created_by, created_by_app)
VALUES ('England', 'ENG', true, 'seed', 'ScoreCast')
ON CONFLICT DO NOTHING;

INSERT INTO scorecast.league_master (name, country_id, sort_order, is_active, created_by, created_by_app)
SELECT 'Premier League', c.id, 1, true, 'seed', 'ScoreCast'
FROM scorecast.country_master c WHERE c.name = 'England'
ON CONFLICT DO NOTHING;

INSERT INTO scorecast.team_master (name, short_name, league_id, country_id, is_active, created_by, created_by_app)
SELECT t.name, t.short_name, l.id, c.id, true, 'seed', 'ScoreCast'
FROM (VALUES
  ('Arsenal', 'ARS'),
  ('Aston Villa', 'AVL'),
  ('AFC Bournemouth', 'BOU'),
  ('Brentford', 'BRE'),
  ('Brighton & Hove Albion', 'BHA'),
  ('Chelsea', 'CHE'),
  ('Crystal Palace', 'CRY'),
  ('Everton', 'EVE'),
  ('Fulham', 'FUL'),
  ('Ipswich Town', 'IPS'),
  ('Leicester City', 'LEI'),
  ('Liverpool', 'LIV'),
  ('Manchester City', 'MCI'),
  ('Manchester United', 'MUN'),
  ('Newcastle United', 'NEW'),
  ('Nottingham Forest', 'NFO'),
  ('Southampton', 'SOU'),
  ('Tottenham Hotspur', 'TOT'),
  ('West Ham United', 'WHU'),
  ('Wolverhampton Wanderers', 'WOL')
) AS t(name, short_name)
CROSS JOIN scorecast.league_master l
CROSS JOIN scorecast.country_master c
WHERE l.name = 'Premier League' AND c.name = 'England'
ON CONFLICT DO NOTHING;
