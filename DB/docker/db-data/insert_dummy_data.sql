INSERT INTO "User" ("id", "email", "isActive")
VALUES
  ('39D3EE56-0309-4B1B-91E7-443CE081B4F8', 'user1@example.com', true),
  ('AF3CE4FC-593E-4808-AE79-C3D5FE5BB723', 'user2@example.com', true);

INSERT INTO "Password" ("userId", "password")
VALUES
  ('39D3EE56-0309-4B1B-91E7-443CE081B4F8', decode('a1b2c3d4e5', 'hex')),
  ('AF3CE4FC-593E-4808-AE79-C3D5FE5BB723', decode('f1e2d3c4b5', 'hex'));
