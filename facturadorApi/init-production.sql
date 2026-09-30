-- Crear usuario facturador_prod_user con permisos completos en facturador_prod
CREATE USER IF NOT EXISTS 'facturador_prod_user'@'%' IDENTIFIED BY 'ProduccionSegura2025!';
GRANT ALL PRIVILEGES ON facturador_prod.* TO 'facturador_prod_user'@'%';
FLUSH PRIVILEGES;
