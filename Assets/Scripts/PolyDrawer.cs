using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent (typeof (MeshFilter),typeof (MeshRenderer))]
public class PolyDrawer : MonoBehaviour {
	
	public List<Vector2> RawPoints;
	public Material Mat;
	
	private struct PolyPoint{

		public int NextP;
		public int PrevP;

		public int NextEar;
		public int PrevEar;

		public int NextRefl;
		public int PrevRefl;

		public bool isEar;

	}

	private List<Vector3> m_TriPointList;
	private int Pointcount;
	private PolyPoint[] PolyPointList;
	
	private Mesh 						m_Mesh;
	private MeshFilter 					m_MeshFilter;
	private MeshRenderer 				m_MeshRenderer;	
	private Vector2[] 					m_Uv;

	void Start () {
	
		Pointcount = RawPoints.Count;
		PolyPointList = new PolyPoint[Pointcount+1];
		m_TriPointList = new List<Vector3>();

		FillLists();

		Triangulate();

		DrawPolygon();
	}

	private void FillLists(){

		/*
		 * three doubly linked lists (points list,reflective points list, ears list) are
		 * maintained in the "PolyPointList" arry.
		 * points list is a cyclic list while other two arent.
		 * 0 index of the Point list is kept only for entering the lists
		 * -1 means undefined link
		 */
		PolyPoint p = new PolyPoint();

		PolyPointList[0] = p;
		PolyPointList[0].NextP = 1;
		PolyPointList[0].PrevP = -1;
		PolyPointList[0].NextEar = -1;
		PolyPointList[0].PrevEar = -1;
		PolyPointList[0].NextRefl = -1;
		PolyPointList[0].PrevRefl = -1;
		PolyPointList[0].isEar = false;

		int T_Reflective = -1;
		int T_Convex = -1;

		for(int i=1;i<=Pointcount;i++){
			
			PolyPointList[i]=p;

			if(i==1)
				PolyPointList[i].PrevP = Pointcount;
			else
				PolyPointList[i].PrevP = i-1;

			PolyPointList[i].NextP = (i%Pointcount)+1;

			if(isReflective(i)){

				PolyPointList[i].PrevRefl = T_Reflective;

				if(T_Reflective==-1){
					PolyPointList[0].NextRefl =i;
				}
				else
					PolyPointList[T_Reflective].NextRefl=i;

				T_Reflective = i;
				PolyPointList[i].NextRefl = -1;

				PolyPointList[i].PrevEar = -1;
				PolyPointList[i].NextEar = -1;

			}
			else{

				PolyPointList[i].PrevRefl = -1;
				PolyPointList[i].NextRefl = -1;
				PolyPointList[i].isEar = true;

				PolyPointList[i].PrevEar = T_Convex;

				if(T_Convex==-1){
					PolyPointList[0].NextEar = i;
				}
				else
					PolyPointList[T_Convex].NextEar=i;

				T_Convex = i;

				PolyPointList[i].NextEar = -1;
			}

		}


		int Con = PolyPointList[0].NextEar;

		while(Con!=-1){

			if(!isCleanEar(Con)){
				RemoveEar(Con);
			}
				Con = PolyPointList[Con].NextEar;

		}


	}


	/*
	 * "Ear Clipping" is used for
	 * Polygon triangulation
	 */
	private void Triangulate(){

		int i;
		
		while(Pointcount>3){

			/*
			 * The Two-Ears Theorem: "Except for triangles every 
			 * simple ploygon has at least two non-overlapping ears"
			 * so there i will always have a value
			 */
			i= PolyPointList[0].NextEar;
			
			int PrevP = PolyPointList[i].PrevP;
			int NextP = PolyPointList[i].NextP;
			
			m_TriPointList.Add(new Vector3(PrevP,i,NextP));
			
			RemoveEar(i);
			RemoveP(i);

			if(!isReflective(PrevP)){
				
				if(isCleanEar(PrevP)){ 
					
					if(!PolyPointList[PrevP].isEar){
						
						AddEar(PrevP);
					}
					
				}
				else{
					
					if(PolyPointList[PrevP].isEar){
						
						RemoveEar(PrevP);
					}  
					
				}
				
			}

			if(!isReflective(NextP)){
				
				if(isCleanEar(NextP)){ 
					
					if(!PolyPointList[NextP].isEar){
						
						AddEar(NextP);
					}
					
				}
				else{
					
					if(PolyPointList[NextP].isEar){
						
						RemoveEar(NextP);
					}  
					
				}
				
			}
			
			
		}

		int y = PolyPointList[0].NextP;
		int x = PolyPointList[y].PrevP;
		int z = PolyPointList[y].NextP;
		
		m_TriPointList.Add(new Vector3(x , y , z));

	}

	

	private void DrawPolygon(){
		
		m_MeshFilter = (MeshFilter)GetComponent(typeof(MeshFilter));
		m_MeshRenderer = (MeshRenderer)GetComponent(typeof(MeshRenderer));
		m_MeshRenderer.GetComponent<Renderer>().material = Mat;
		m_Mesh = m_MeshFilter.mesh;

		int vertex_count = RawPoints.Count;
		int triangle_count = m_TriPointList.Count;

		/*
		 * Mesh vertices
		 */
		Vector3 [] vertices = new Vector3 [vertex_count]; 

		for(int i=0;i<vertex_count;i++){
			
			vertices[i] = RawPoints[i];
		}

		RawPoints.Clear();

		m_Mesh.vertices = vertices;

		/*
		 * Mesh trangles
		 */
		int [] tri = new int [triangle_count*3];
		
		for(int i=0,j=0;i<triangle_count;i++,j+=3){
			
			tri[j]=(int)(m_TriPointList[i].x-1);
			tri[j+1]=(int)(m_TriPointList[i].y-1);
			tri[j+2]=(int)(m_TriPointList[i].z-1);
			
		}

		m_Mesh.triangles = tri;

		/*
		 * Mesh noramals
		 */
		Vector3[] normals= new Vector3[vertex_count];

		for(int i=0;i<vertex_count;i++){
			normals[i] = -Vector3.forward;
		}
		
		m_Mesh.normals = normals;

		/*
		 * Mesh UVs
		 */
		m_Uv    = new Vector2[vertex_count];
		
		for(int i=0;i<m_Uv.Length;i++){
			m_Uv[i] = new Vector2(0, 0);
		}
		
		
		m_Mesh.uv = m_Uv;

	}



	/*
	 * Utility Methods
	 */

	private bool isCleanEar(int Ear){

		/*
		 * Barycentric Technique is used to test
		 * if the reflective vertices are in selected ears
		 */

		float dot00;
		float dot01;
		float dot02;
		float dot11;
		float dot12;

		float invDenom;
		float U;
		float V;

		Vector2 v0 = RawPoints[PolyPointList[Ear].PrevP-1]-RawPoints[Ear-1];
		Vector2 v1 = RawPoints[PolyPointList[Ear].NextP-1]-RawPoints[Ear-1];
		Vector2 v2;

		int i = PolyPointList[0].NextRefl;

		while(i!=-1){

			v2 = RawPoints[i-1]-RawPoints[Ear-1];

			dot00=Vector2.Dot(v0,v0);
			dot01=Vector2.Dot(v0,v1);
			dot02=Vector2.Dot(v0,v2);
			dot11=Vector2.Dot(v1,v1);
			dot12=Vector2.Dot(v1,v2);

			invDenom = 1 / (dot00 * dot11 - dot01 * dot01);
			U = (dot11 * dot02 - dot01 * dot12) * invDenom;
			V = (dot00 * dot12 - dot01 * dot02) * invDenom;

			if((U > 0) && (V > 0) && (U + V < 1))
			return false;

			i = PolyPointList[i].NextRefl;
		}

		return true;
	}

	private bool isReflective(int P){

		/*
		 * vector cross product is used to determin the reflectiveness of vertices
		 * because "Sin" values of angles are always - if the angle > 180 
		 */

		Vector2 v0 = RawPoints[PolyPointList[P].PrevP-1]- RawPoints[P-1];
		Vector2 v1 = RawPoints[PolyPointList[P].NextP-1]- RawPoints[P-1];

		Vector3 A = Vector3.Cross(v0,v1);

		if(A.z<0)
			return true;
	
		return false;
	}
	
	private void RemoveEar(int Ear){

		int PrevEar = PolyPointList[Ear].PrevEar;
		int NextEar = PolyPointList[Ear].NextEar;

		PolyPointList[Ear].isEar = false;

		if(PrevEar==-1){
			PolyPointList[0].NextEar = NextEar;
		}
		else{
			PolyPointList[PrevEar].NextEar = NextEar;
		}
		
		if(NextEar!=-1){
			PolyPointList[NextEar].PrevEar = PrevEar;
		}
	}

	private void AddEar(int Ear){

		int NextEar=PolyPointList[0].NextEar;

		PolyPointList[0].NextEar = Ear;
		
		PolyPointList[Ear].PrevEar = -1;
		PolyPointList[Ear].NextEar = NextEar;

		PolyPointList[Ear].isEar = true;

		if(NextEar!=-1){

			PolyPointList[NextEar].PrevEar = Ear;

		}
	
	}

	private void RemoverReflective(int P){

		int PrevRefl = PolyPointList[P].PrevRefl;
		int NextRefl = PolyPointList[P].NextRefl;
		
		if(PrevRefl==-1){
			PolyPointList[0].NextRefl = NextRefl;
		}
		else{
			PolyPointList[PrevRefl].NextRefl = NextRefl;
		}
		
		if(NextRefl!=-1){
			PolyPointList[NextRefl].PrevRefl = PrevRefl;
		}

	}

	private void AddReflective(int P){

		int NextRefl=PolyPointList[0].NextRefl;
		
		PolyPointList[0].NextRefl = P;
		
		PolyPointList[P].PrevRefl = -1;
		PolyPointList[P].NextRefl = NextRefl;
		
		if(NextRefl!=-1){
			
			PolyPointList[NextRefl].PrevRefl = P;
			
		}

	}
	
	private void RemoveP(int P){

		int NextP = PolyPointList[P].NextP;
		int PrevP = PolyPointList[P].PrevP;

		PolyPointList[PrevP].NextP=NextP;
		PolyPointList[NextP].PrevP=PrevP;

		if(PolyPointList[0].NextP==P)
			PolyPointList[0].NextP=NextP;

		--Pointcount;
	}


}
