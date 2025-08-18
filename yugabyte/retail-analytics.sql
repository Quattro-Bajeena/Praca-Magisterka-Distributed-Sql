CREATE DATABASE yb;
GRANT ALL ON DATABASE yb to yugabyte;
\c yb;
\i share/schema.sql;
\i share/products.sql;
\i share/users.sql;
\i share/orders.sql;
\i share/reviews.sql;
\d products;
SELECT count(*) FROM products;
SELECT id, title, category, price, rating
          FROM products
          LIMIT 5;
SELECT id, title, category, price, rating
          FROM products
          LIMIT 3 OFFSET 5;

SELECT users.id, users.name, users.email, orders.id, orders.total
          FROM orders INNER JOIN users ON orders.user_id=users.id
          LIMIT 10;

SELECT id, category, price, quantity FROM products WHERE id=2;


BEGIN TRANSACTION;
/* First insert a new order into the orders table. */
INSERT INTO orders
  (id, created_at, user_id, product_id, discount, quantity, subtotal, tax, total)
VALUES (
  (SELECT max(id)+1 FROM orders)                 /* id */,
  now()                                          /* created_at */,
  1                                              /* user_id */,
  2                                              /* product_id */,
  0                                              /* discount */,
  10                                             /* quantity */,
  (10 * (SELECT price FROM products WHERE id=2)) /* subtotal */,
  0                                              /* tax */,
  (10 * (SELECT price FROM products WHERE id=2)) /* total */
) RETURNING id;

/* Next decrement the total quantity from the products table. */
UPDATE products SET quantity = quantity - 10 WHERE id = 2;

COMMIT;



SELECT DISTINCT(source) FROM users;
SELECT MIN(price), MAX(price), AVG(price) FROM products;


SELECT source, count(*) AS num_user_signups
          FROM users
          GROUP BY source
          ORDER BY num_user_signups DESC;

SELECT source, ROUND(SUM(orders.total)) AS total_sales
          FROM users LEFT JOIN orders ON users.id=orders.user_id
          GROUP BY source
          ORDER BY total_sales DESC;


CREATE VIEW channel AS
            (SELECT source, ROUND(SUM(orders.total)) AS total_sales
             FROM users LEFT JOIN orders ON users.id=orders.user_id
             GROUP BY source
             ORDER BY total_sales DESC);


\d

SELECT source,
            total_sales * 100.0 / (SELECT SUM(total_sales) FROM channel) AS percent_sales
          FROM channel
          WHERE source='Facebook';