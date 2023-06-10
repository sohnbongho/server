SELECT * FROM gamedb.tbl_user;

CREATE TABLE `tbl_user` (
  `seq` bigint(20) NOT NULL AUTO_INCREMENT COMMENT '일련번호',
  `user_uid` bigint(20) DEFAULT '0' COMMENT 'user uid',
  `user_id` varchar(32) NOT NULL COMMENT 'user Id',
  `level` int(11) NOT NULL COMMENT '공지 내용',
  PRIMARY KEY (`seq`)
) ENGINE=InnoDB AUTO_INCREMENT=3 DEFAULT CHARSET=utf8;

