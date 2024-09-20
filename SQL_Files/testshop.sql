-- phpMyAdmin SQL Dump
-- version 3.5.1
-- http://www.phpmyadmin.net
--
-- Хост: 127.0.0.1
-- Время создания: Сен 20 2024 г., 17:11
-- Версия сервера: 5.5.25
-- Версия PHP: 5.3.13

-- База данных: `testshop`
--
USE testshop;
-- --------------------------------------------------------
--
-- Структура таблицы `cart`
--

CREATE TABLE IF NOT EXISTS `cart` (
  `idOrder` int(11) NOT NULL,
  `idProduct` int(11) NOT NULL,
  `price` decimal(8,2) NOT NULL,
  `quantity` int(11) NOT NULL,
  FOREIGN KEY(idOrder) REFERENCES orders(idOrder),
  FOREIGN KEY(idProduct) REFERENCES products(idProduct)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- --------------------------------------------------------

--
-- Структура таблицы `orders`
--

CREATE TABLE IF NOT EXISTS `orders` (
  `idOrder` int(11) NOT NULL,
  `idUser` int(11) NOT NULL,
  `cityAddres` int(11) DEFAULT NULL,
  `zipCode` int(11) DEFAULT NULL,
  `sum` decimal(7,2) NOT NULL,
  `reg_date` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `executed_date` timestamp NULL DEFAULT NULL,
  PRIMARY KEY (`idOrder`),
  FOREIGN KEY (idUser) REFERENCES users(idUser)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- --------------------------------------------------------

--
-- Структура таблицы `products`
--

CREATE TABLE IF NOT EXISTS `products` (
  `idProduct` int(11) NOT NULL AUTO_INCREMENT,
  `name` varchar(60) NOT NULL,
  `description` text,
  `quantity` int(11) NOT NULL,
  `price` decimal(6,2) NOT NULL,
  `barcode` varchar(20) DEFAULT NULL,
  PRIMARY KEY (`idProduct`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8 AUTO_INCREMENT=1 ;

-- --------------------------------------------------------

--
-- Структура таблицы `users`
--

CREATE TABLE IF NOT EXISTS `users` (
  `idUser` int(11) NOT NULL AUTO_INCREMENT,
  `username` varchar(30) NOT NULL,
  `password_hash` char(60) NOT NULL,
  `first_name` varchar(30) NOT NULL,
  `last_name` varchar(30) NOT NULL,
  `middle_name` varchar(30) DEFAULT NULL,
  `phone_number` varchar(11) DEFAULT NULL,
  `email` varchar(60) DEFAULT NULL,
  `created_at` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`idUser`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8 AUTO_INCREMENT=1 ;
