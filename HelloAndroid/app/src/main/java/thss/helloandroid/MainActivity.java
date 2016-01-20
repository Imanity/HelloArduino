package thss.helloandroid;

import android.app.Activity;
import android.content.BroadcastReceiver;
import android.content.Context;
import android.content.Intent;
import android.content.IntentFilter;
import android.graphics.Canvas;
import android.graphics.Color;
import android.graphics.Paint;
import android.os.Bundle;
import android.util.Log;
import android.view.Display;
import android.view.View;
import android.view.WindowManager;
import android.widget.TextView;

public class MainActivity extends Activity {
    private double roll = 0, pitch = 0, yaw = 0;
    private Vector3D[] vector;
    private int screenWidth = 0, screenHeight = 0;

    private MyView view;

    public void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        view = new MyView(this);
        view.setBackgroundColor(Color.WHITE);
        setContentView(view);
        //初始化
        vector = new Vector3D[5];
        for (int i = 0; i < 5; ++i) {
            vector[i] = new Vector3D(0, 0, 0);
        }
        testData(); //Delete it after test
        //获取屏幕分辨率
        WindowManager windowManager = getWindowManager();
        Display display = windowManager.getDefaultDisplay();
        screenWidth = display.getWidth();
        screenHeight = display.getHeight();
        //接收广播
        Intent i = new Intent(MainActivity.this, MyService.class);
        startService(i);
        IntentFilter intentFilter = new IntentFilter("outputAction");
        MyBroadCastReceiver myBroadCastReceiver = new MyBroadCastReceiver();
        registerReceiver(myBroadCastReceiver, intentFilter);
    }

    //获得并解析蓝牙传输信息
    private class MyBroadCastReceiver extends BroadcastReceiver {
        @Override
        public void onReceive(Context context, Intent intent) {
            Bundle myBundle = intent.getExtras();
            String info = myBundle.getString("str");
            if (info.indexOf('<') == -1 || info.indexOf('>') == -1 || info.indexOf('<') >= info.lastIndexOf('>')) { //接收信息不符合数据格式
                //myTextView.setText(info);
            } else {
                String detail = info.substring(info.indexOf('<'), info.lastIndexOf('>'));
                String data[] = detail.split("<");
                for (int i = 0; i < data.length; ++i) {
                    if (data[i].length() == 0)
                        continue;
                    data[i] = data[i].substring(0, data[i].length() - 1);
                    String key[] = data[i].split(":");
                    if (key[0].length() == 1) {
                        switch (key[0].charAt(0)) {
                            case 'x': roll = Double.valueOf(key[1]); break;
                            case 'y': pitch = Double.valueOf(key[1]); break;
                            case 'z': yaw = Double.valueOf(key[1]); break;
                            default: break;
                        }
                    }
                }
            }
            vector[1] = Translator.rpy_to_xyz(new Vector3D(roll, pitch, yaw));
            view.postInvalidate();
        }
    }

    //绘图类
    class MyView extends View {
        Paint paint = null;
        public MyView(Context context) {
            super(context);
            paint = new Paint();
        }
        public void onDraw(Canvas canvas) {
            //输出文字信息
            paint.setTextSize(25);
            paint.setStrokeWidth(3);
            canvas.drawText("roll:" + roll + "\tpitch:" + pitch + "\tyaw:" + yaw, 20, 20, paint);
            canvas.drawText("x:" + vector[1].x, 20, 60, paint);
            canvas.drawText("y:" + vector[1].y, 20, 80, paint);
            canvas.drawText("z:" + vector[1].z, 20, 100, paint);
            super.onDraw(canvas);
            //绘制火柴人
            float centerX = screenWidth / 2;
            float centerY = screenHeight / 2;
            float singleLength = screenWidth / 5;
            //头部
            canvas.drawCircle(centerX, centerY - 30, 30, paint);
            //躯干
            canvas.drawLine(centerX, centerY, centerX + (float) vector[0].x * singleLength * 2, centerY - (float) vector[0].z * singleLength * 2, paint);
            //大臂
            float tmpX1 = centerX + (float)vector[1].x * singleLength;
            float tmpX2 = centerX + (float)vector[2].x * singleLength;
            float tmpY1 = centerY - (float)vector[1].z * singleLength;
            float tmpY2 = centerY - (float)vector[2].z * singleLength;
            canvas.drawLine(centerX, centerY, tmpX1, tmpY1, paint);
            canvas.drawLine(centerX, centerY, tmpX2, tmpY2, paint);
            //小臂
            canvas.drawLine(tmpX1, tmpY1, tmpX1 + (float)vector[3].x * singleLength, tmpY1 - (float)vector[3].z * singleLength, paint);
            canvas.drawLine(tmpX2, tmpY2, tmpX2 + (float)vector[4].x * singleLength, tmpY2 - (float)vector[4].z * singleLength, paint);
        }
    }

    //测试用,测试后删除
    private void testData() {
        //躯干
        vector[0].x = 0;
        vector[0].y = 0;
        vector[0].z = -1;
        //左大臂
        //Modified during broadcasting
        //右大臂
        vector[2].x = -1;
        vector[2].y = 0;
        vector[2].z = 0;
        //左小臂
        vector[3].x = 0;
        vector[3].y = 0;
        vector[3].z = -1;
        //右小臂
        vector[4].x = 0;
        vector[4].y = 0;
        vector[4].z = 1;
    }
}
