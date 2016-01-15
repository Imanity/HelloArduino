import java.util.*;
import java.text.*;

public class Vector3D{
    public double x, y, z; //roll, pitch, yaw
    public Vector3D(double x_, double y_, double z_){
        x = x_;
        z = z_;
        y = y_;
        return;
    }
    String stringify(){
        DecimalFormat df = new DecimalFormat("0.00");
        String str = new String();
        str += '(';
        str += df.format(x);
        str += ',';
        str += df.format(y);
        str += ',';
        str += df.format(z);
        str += ')';
        return str;
    }
}
