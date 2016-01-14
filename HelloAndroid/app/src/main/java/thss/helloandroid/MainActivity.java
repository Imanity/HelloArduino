package thss.helloandroid;

import java.io.IOException;
import java.io.InputStream;
import java.io.OutputStream;
import java.util.Iterator;
import java.util.Set;
import java.util.UUID;

import android.app.Activity;
import android.bluetooth.BluetoothAdapter;
import android.bluetooth.BluetoothDevice;
import android.bluetooth.BluetoothSocket;
import android.content.BroadcastReceiver;
import android.content.Context;
import android.content.Intent;
import android.content.IntentFilter;
import android.os.Bundle;
import android.util.Log;
import android.view.View;
import android.view.View.OnClickListener;
import android.widget.Button;
import android.widget.TextView;

public class MainActivity extends Activity {
    private double x, y, z;
    private Button mybutton = null;
    private TextView myTextView = null;

    public void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_main);
        //得到按钮
        mybutton = (Button)findViewById(R.id.btn2);
        myTextView = (TextView)findViewById(R.id.text);
        myTextView.setText("No Output!");
        //绑定监听器
        mybutton.setOnClickListener(new ButtonListener());
        Intent i = new Intent(MainActivity.this, MyService.class);
        startService(i);
        IntentFilter intentFilter = new IntentFilter("outputAction");
        MyBroadCastReceiver myBroadCastReceiver = new MyBroadCastReceiver();
        registerReceiver(myBroadCastReceiver, intentFilter);
    }

    //监听器匿名类
    private class ButtonListener implements OnClickListener {
        public void onClick(View v) {
            myTextView.setText("Button pushed!");
        }
    }

    private class MyBroadCastReceiver extends BroadcastReceiver {
        @Override
        public void onReceive(Context context, Intent intent) {
            Bundle myBundle = intent.getExtras();
            String info = myBundle.getString("str");
            if (info.indexOf('<') == -1 || info.indexOf('>') == -1) {
                return;
            }
            String detail = info.substring(info.indexOf('<'), info.lastIndexOf('>'));
            Log.d("thss1", detail);
            String data[] = detail.split("<");
            for (int i = 0; i < data.length; ++i) {
                if (data[i].length() == 0)
                    continue;
                data[i] = data[i].substring(0, data[i].length() - 1);
                Log.d("thss1", data[i]);
                String key[] = data[i].split(":");
                if (key[0].length() == 1) {
                    switch (key[0].charAt(0)) {
                        case 'x': x = Double.valueOf(key[1]); break;
                        case 'y': y = Double.valueOf(key[1]); break;
                        case 'z': z = Double.valueOf(key[1]); break;
                        default: break;
                    }
                }
            }
            myTextView.setText("x:" + x + "\ty:" + y + "\tz:" + z);
        }
    }
}
