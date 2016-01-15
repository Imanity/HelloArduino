public class Test{
  public static void main(String args[]){
    Translator t = new Translator();
    Vector3D v = new Vector3D(2, 23, 233);
    System.out.println(v.stringify()); //before translatoin
    System.out.println(t.translate(v).stringify()); //after translation
  }
}
