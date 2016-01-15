public class Translator{
  public static void hello(){
    System.out.println("hello, world. i am the translator.");
  }
  public static Vector3D translate(Vector3D rollPitchYaw){
    double roll = rollPitchYaw.x;
    double pitch = rollPitchYaw.y;
    double yaw = rollPitchYaw.z;
    double x, y, z;
    x = roll * 2;
    y = pitch * 3;
    z = yaw * 4;
    return new Vector3D(x, y, z);
  }
}
