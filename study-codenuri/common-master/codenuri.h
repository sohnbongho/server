// module      : codenuri.h
//               codenuri.c
// description : C ���� ������ ����� �ҽ� �Դϴ�.
// version     : 0.5

// home : cafe.naver.com/codenuri
// mail : codenuri@naver.com

#ifndef CODENURI_H_
#define CODENURI_H_

#pragma once

#define _CRT_SECURE_NO_WARNINGS

void cn_set_cursor_pos(int x, int y);
void cn_get_cursor_pos(int *px, int *py);
void cn_hide_cursor();
void cn_show_cursor();

#define xyprintf(x, y, ... )  do{ cn_set_cursor_pos(x,y); printf(__VA_ARGS__ );}while(0);
#define sleep(ms)             cn_sleep( (ms))
#define clear()	              cn_clrscr()
#define hidecursor()  cn_hide_cursor()
#define showcursor()  cn_show_cursor()

// �� ��

#endif // CODENURI_H_

