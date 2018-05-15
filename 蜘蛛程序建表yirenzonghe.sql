/*
Navicat MySQL Data Transfer

Source Server         : 本机MySql
Source Server Version : 50718
Source Host           : localhost:3306
Source Database       : yirenzonghe

Target Server Type    : MYSQL
Target Server Version : 50718
File Encoding         : 65001

Date: 2017-10-16 10:02:47
*/

SET FOREIGN_KEY_CHECKS=0;

-- ----------------------------
-- Table structure for picture
-- ----------------------------
DROP TABLE IF EXISTS `picture`;
CREATE TABLE `picture` (
  `ID` int(11) NOT NULL AUTO_INCREMENT,
  `Type1` varchar(255) CHARACTER SET gbk DEFAULT NULL,
  `Type2` varchar(255) CHARACTER SET gbk DEFAULT NULL,
  `Title` varchar(255) CHARACTER SET gbk DEFAULT NULL,
  `URL` varchar(255) CHARACTER SET gbk DEFAULT NULL,
  `Content` text CHARACTER SET gbk,
  `Count` int(11) DEFAULT NULL,
  `col1` varchar(255) DEFAULT NULL,
  `col2` varchar(255) DEFAULT NULL,
  `col3` varchar(255) DEFAULT NULL,
  `col4` varchar(255) DEFAULT NULL,
  `col5` varchar(255) DEFAULT NULL,
  PRIMARY KEY (`ID`),
  KEY `picture_id_type2` (`ID`,`Type2`)
) ENGINE=InnoDB AUTO_INCREMENT=194590 DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for story
-- ----------------------------
DROP TABLE IF EXISTS `story`;
CREATE TABLE `story` (
  `ID` int(11) NOT NULL AUTO_INCREMENT,
  `Type1` varchar(255) DEFAULT NULL,
  `Type2` varchar(255) DEFAULT NULL,
  `Title` varchar(255) DEFAULT NULL,
  `URL` varchar(255) DEFAULT NULL,
  `Content` text,
  `Count` int(11) DEFAULT NULL,
  `col1` varchar(255) DEFAULT NULL,
  `col2` varchar(255) DEFAULT NULL,
  `col3` varchar(255) DEFAULT NULL,
  `col4` varchar(255) DEFAULT NULL,
  `col5` text,
  PRIMARY KEY (`ID`),
  KEY `story_id_type2` (`ID`,`Type2`)
) ENGINE=InnoDB AUTO_INCREMENT=31892 DEFAULT CHARSET=gbk;

-- ----------------------------
-- Table structure for video
-- ----------------------------
DROP TABLE IF EXISTS `video`;
CREATE TABLE `video` (
  `ID` int(11) NOT NULL AUTO_INCREMENT,
  `Type1` varchar(255) DEFAULT NULL,
  `Type2` varchar(255) DEFAULT NULL,
  `Title` varchar(255) DEFAULT NULL,
  `URL` varchar(255) DEFAULT NULL,
  `Content` text,
  `Count` int(11) DEFAULT NULL,
  `col1` varchar(255) DEFAULT NULL,
  `col2` varchar(255) DEFAULT NULL,
  `col3` varchar(255) DEFAULT NULL,
  `col4` varchar(255) DEFAULT NULL,
  `col5` text,
  PRIMARY KEY (`ID`),
  KEY `story_id_type2` (`ID`,`Type2`)
) ENGINE=InnoDB AUTO_INCREMENT=39152 DEFAULT CHARSET=gbk;
