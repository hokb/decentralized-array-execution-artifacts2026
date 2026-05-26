    subroutine SKMEANS(X,M,N,IT,K,classes) bind(C, name="SKMEANS")
      use iso_c_binding
    !USE KERNEL32
    !DEC$ ATTRIBUTES DLLEXPORT::SKMEANS 

      ! DUMMIES
      INTEGER(KIND=8) :: M,N
      INTEGER(Kind=4) :: K,IT
      DOUBLE PRECISION, INTENT(IN) :: X(M,N)
      INTEGER(Kind=8), INTENT(OUT) :: classes(N)
      ! LOCALS 
      integer(kind=8),ALLOCATABLE :: oldClasses(:)
      DOUBLE PRECISION,ALLOCATABLE :: centers(:,:) &
                       ,distances(:) & 
                       ,tmpCenter(:) & 
                       ,distArr(:,:)
      DOUBLE PRECISION nan, step 
      INTEGER S
   
      nan = 0
      nan = nan / nan
      IT = 0
   
      ALLOCATE(centers(M,K),oldClasses(N),distances(K),tmpCenter(M),distArr(M,K))  
 
      ! extract initial centers from given samples, equally spaced (corresp.: X[:,linspace(...)]
      step = (N-2) / float(K - 1)
      distances = [( 1. + step * i, i=0, K-1)] 
      centers(:,:) = X(:,INT(distances))                ! init centers: K data points 
      
      classes = 0                                       ! clear incoming classes
      
      do                                                ! the main loop     
        oldClasses = classes;                           ! save for exit condition
        IT = IT + 1;                                    ! increment iteration counter

        do i = 1, N                                     ! for each sample i...
            do j = 1, K                                 
                distArr(:,j) = X(:,i) - centers(:,j)    
            end do
            distArr = distArr * distArr                 ! ... find its nearest cluster
            distances(:) = SQRT(sum(distArr,1))     
            classes(i) = minloc ( distances, dim=1)     ! update class for sample i
        end do
   
        do j = 1,K                                      ! for each cluster j: compute mean
            tmpCenter = 0; 
            S = 0; 
            do i = 1,N                                  ! sum all samples assigned to cluster j
                if (classes(i) == j) then
                    tmpCenter = tmpCenter + X(:,i); 
                    S = S + 1;                          ! increment 'samples in cluster' count
                end if     
            end do
            if (S > 0) then 
                centers(:,j) = tmpCenter / S;         ! mean
            else 
                centers(:,j) = nan;                   ! empty cluster
            end if 
        end do
     
        if (IT .GE. 20) then                            ! maxiteration: 20 (shall never occur) 
            exit; 
        end if 
        if (all(classes == oldClasses)) then            ! exit condition: unchanged class assignments 
            exit;  
        end if 
    
      end do
      DEALLOCATE(centers, oldClasses,distances,tmpCenter,distArr);
    end subroutine SKMEANS
        
    