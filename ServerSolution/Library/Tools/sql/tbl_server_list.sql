USE danceparty;

DROP TABLE IF EXISTS `tbl_server_list`;
CREATE TABLE `tbl_server_list` (
  `server_id` int(11) NOT NULL,
  `server_type` tinyint(4) DEFAULT null COMMENT '1:로그인 서버, 2: 로비 서버, 3: 존 서버',
  `server_name` varchar(96) NOT NULL DEFAULT '' COMMENT '서버명',
  `ipaddr` varchar(48) DEFAULT NULL COMMENT 'ip',
  `port` int(11) DEFAULT NULL COMMENT 'port',  
  `level_min` int(11) DEFAULT NULL COMMENT '최소 레벨',
  `level_max` int(11) DEFAULT NULL COMMENT '최대 레벨',
  `parameter` varchar(384) DEFAULT NULL,
  PRIMARY KEY (`server_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COMMENT='서버 리스트 테이블';


INSERT INTO danceparty.tbl_server_list (server_id, server_type, server_name, ipaddr, port, level_min, level_max, `parameter`) VALUES(1, 1, 'LoginServer', '127.0.0.1', 16101, 0, 100, 'void'),
(2, 2, 'LobbyServer1', '127.0.0.1', 16201, 0, 100, 'void'),
(3, 3, 'ZoneServer1', '127.0.0.1', 16301, 0, 100, 'void');

