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
        IntentFilter intentFilter = new IntentFilter("android.intent.action.cmdactivity");
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
            String myStr = myBundle.getString("str");
            myTextView.setText(myStr);
        }
    }
}
