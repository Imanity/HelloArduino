public class Vector3D{
    public double x, y, z;
    public Vector3D(double x_, double y_, double z_){
        x = x_;
        z = z_;
        y = y_;
        return;
    }
    String stringify(){
        String str = new String();
        str += '(';
        str += x;
        str += ',';
        str += y;
        str += ',';
        str += z;
        str += ')';
        return str;
    }
}
