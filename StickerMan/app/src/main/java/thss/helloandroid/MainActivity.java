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
import android.widget.Toast;

public class MainActivity extends Activity {
    private Vector3D[] rollPitchYaw;
    private Vector3D[] vector;
    private int screenWidth = 0, screenHeight = 0;

    private MyView view;


    public void showToast(String str){
        Toast.makeText(this, str, Toast.LENGTH_SHORT).show();
    }

    public void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        view = new MyView(this);
        view.setBackgroundColor(Color.WHITE);
        setContentView(view);

        //成员变量初始化
        vector = new Vector3D[4];
        rollPitchYaw = new Vector3D[4];
        for (int i = 0; i < 4; ++i) {
            vector[i] = new Vector3D(0, 0, 0);
        }
        for (int i = 0; i < 4; ++i) {
            rollPitchYaw[i] = new Vector3D(0, 0, 0);
        }

        //testData(); //TODO: Delete it after test


        //获取屏幕分辨率
        WindowManager windowManager = getWindowManager();
        Display display = windowManager.getDefaultDisplay();
        screenWidth = display.getWidth();
        screenHeight = display.getHeight();

        //接收广播
        Intent i = new Intent(MainActivity.this, MyService.class);
        startService(i);
        registerReceiver(new BluetoothChunkReceiver(), new IntentFilter("bluetoothChunk"));
        registerReceiver(new BluetoothToastReceiver(), new IntentFilter("bluetoothToast"));
    }

    //获得并解析蓝牙传输信息
    private class BluetoothChunkReceiver extends BroadcastReceiver {

        private String LOG_TAG = "bluetooth chunk";

        @Override
        public void onReceive(Context context, Intent intent) {
            Bundle myBundle = intent.getExtras();
            String info = myBundle.getString("str");
            Log.v(LOG_TAG, "GET: " + info);

            if(parseChunk(info) > 0) {
                //更新视图
                view.postInvalidate();
            }

        }

        public int parseChunk(String chunk) {
            char ch;
            char lower = 0;
            boolean lowerAssigned = false;
            for (int i = 0; i < chunk.length(); ++i) {
                ch = chunk.charAt(i);
                if ((ch & (1 << 6)) != 0) { // 01000000 as mask
                    if (!lowerAssigned)
                        continue;
                    char higher = (char)(ch & 3); // 00000011 as mask
                    char id = (char)((ch & 60) >> 2); // 00111100 as mask
                    int value = higher * 64 + lower;
                    Log.v(LOG_TAG, "BIN: " + Integer.toBinaryString((int)ch));
                    boolean isOutOfBound = false;
                    switch (id % 3) {
                        case 0:
                            rollPitchYaw[id / 3].x = value * 1.5 - 180;
                            break;
                        case 1:
                            rollPitchYaw[id / 3].y = value * 1.5 - 180;
                            break;
                        case 2:
                            rollPitchYaw[id / 3].z = value * 1.5 - 180;
                            break;
                        default:
                            isOutOfBound = true;
                            break;
                    }
                    if (isOutOfBound) {
                        Log.e(LOG_TAG, "Message Err: " + Integer.toBinaryString((int)ch));
                        continue;
                    }
                    lowerAssigned = false;
                } else {
                    lower = ch;
                    lowerAssigned = true;
                }
            }
            for(int i = 0; i < 4; i++)
                vector[i] = Translator.rpy_to_xyz(rollPitchYaw[i]);
            return 1;
        }



        //private String lastChunk = ""; //上一个数据块中最后一个 '>' 之后的部分

        /*
        //解析数据块 `><x:2.33><y:6.66><z:`，返回成功解析的数据个数
        public int parseChunk(String chunk){

            chunk = lastChunk + chunk;

            int left = chunk.indexOf('<');
            int right = chunk.lastIndexOf('>');

            if (left == -1) {
                if(right == -1)
                    lastChunk = "";
                else
                    lastChunk = chunk.substring(right + 1);
                Log.d(LOG_TAG, "invalid data chunk");
                return 0;
            } else {
                String numbers = chunk.substring(left, right + 1);
                lastChunk = chunk.substring(right + 1);
                return parseNumbers(numbers);
            }
        }

        //解析多个数字 `<x:2.33><y:6.66>`
        public int parseNumbers(String numbers){
            String data[] = numbers.split("<");
            int sum=0;
            for (int i = 0; i < data.length; ++i) {
                if (data[i].length() == 0)
                    continue;
                sum += parseNumber(data[i]);
            }
            return sum;
        }

        //解析单个数字 `<x:2.33>`
        public int parseNumber(String number){
            number = number.substring(0, number.length() - 1);
            String key[] = number.split(":");
            if (key[0].length() == 1) {
                char chr = key[0].charAt(0);

                int i = ('z' - chr) / 3;

                //x,y,z -> 0
                //u,v,w -> 1
                //r,s,t -> 2
                //...


                i += 1; //仅供测试

                switch (chr % 3) {
                    case 0: rollPitchYaw[i].x = Double.valueOf(key[1]); break;
                    case 1: rollPitchYaw[i].y = Double.valueOf(key[1]); break;
                    case 2: rollPitchYaw[i].z = Double.valueOf(key[1]); break;
                    default: return 0;
                }

                int x[]={1, 2, 3, 4};
                for(int j : x)//仅供测试
                //for(int j=0; j<5; j++)
                    vector[j] = Translator.rpy_to_xyz(rollPitchYaw[j]);
                return 1;
            }
            return 0;
        }
        */
    }

    private class BluetoothToastReceiver extends BroadcastReceiver {
        @Override
        public void onReceive(Context context, Intent intent) {
            Bundle myBundle = intent.getExtras();
            String info = myBundle.getString("str");
            Log.v("bluetooth toast", info);
            showToast(info);
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
            canvas.drawText("rpy1" + rollPitchYaw[0].stringify(), 20, 20, paint);
            canvas.drawText("rpy2" + rollPitchYaw[1].stringify(), 20, 40, paint);
            canvas.drawText("rpy3" + rollPitchYaw[2].stringify(), 20, 60, paint);
            canvas.drawText("rpy4" + rollPitchYaw[3].stringify(), 20, 80, paint);
            canvas.drawText("xyz1" + vector[0].stringify(), 20, 100, paint);
            canvas.drawText("xyz2" + vector[1].stringify(), 20, 120, paint);
            canvas.drawText("xyz3" + vector[2].stringify(), 20, 140, paint);
            canvas.drawText("xyz4" + vector[3].stringify(), 20, 160, paint);
            super.onDraw(canvas);

            //绘制火柴人
            float d = screenWidth / 5;
            float e = 4 * d;

            float singleLength = screenWidth / 5;


            canvas.drawLine(d, d, d + (float)vector[0].x * singleLength, d - (float)vector[0].z * singleLength, paint);
            canvas.drawLine(d, e, d + (float)vector[1].x * singleLength, e - (float)vector[1].z * singleLength, paint);
            canvas.drawLine(e, e, e + (float)vector[2].x * singleLength, e - (float)vector[2].z * singleLength, paint);
            canvas.drawLine(e, d, e + (float)vector[3].x * singleLength, d - (float)vector[3].z * singleLength, paint);
        }
    }

    /*
    private void testData() {
        //躯干
        vector[0].x = 0;
        vector[0].y = 0;
        vector[0].z = -1;
        //左大臂
        vector[1].x = 1;
        vector[1].y = 0;
        vector[1].z = 0;
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
    */
}
