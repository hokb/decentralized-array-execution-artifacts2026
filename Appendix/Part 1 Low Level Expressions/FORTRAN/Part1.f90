!
!   ILNumerics Accelerator Comparison, 2024
!   This program measures the performance of repeatedly 
!   executing the array expression 'sum((m0 & (A << shift)) | (~m0 & B), dim: 1)'
!   in Fortran for comparison with ILNumerics Accelerator and other platforms. 
!    
!   This is the Fortran part. See here for details: 
!   https://ilnumerics.net/accelerate-sum-examples.html
!
!****************************************************************************

    program Getting_Started_ACC_I_FORTRAN
    implicit none

    ! Variables
    INTEGER(4) :: M0,shift, AV, BV, i1, i2, i3, i4, rep = 1000, outerI, j
    integer(4), ALLOCATABLE :: A(:,:,:,:) 
    integer(4), ALLOCATABLE :: B(:,:,:,:) 
    integer(4), ALLOCATABLE :: R(:,:,:) 
    integer(4), ALLOCATABLE :: Temp(:,:,:,:) 
    integer(8) :: startT, endT, clockrate, wallT
    integer :: timesfile, resultsfile, ios
    character(len=256) :: timesfilename, resultsfilename, tempstr
#ifdef OPTIM
    character(len=*), parameter   :: def_timesfilename = 'times_fortran_opt.txt'
    character(len=*), parameter   :: def_resultsfilename = 'results_fortran_opt.bin'
#else
    character(len=*), parameter   :: def_timesfilename = 'times_fortran_noopt.txt'
    character(len=*), parameter   :: def_resultsfilename = 'results_fortran_noopt.bin'
#endif
    

  ! Read output file name from the first command-line argument
    call get_command_argument(1, tempstr)
    if (len_trim(tempstr) > 0) then
        timesfilename = tempstr
    else 
        timesfilename = def_timesfilename
    end if
    call get_command_argument(2, tempstr)
    if (len_trim(tempstr) > 0) then
        resultsfilename = tempstr
    else 
        resultsfilename = def_resultsfilename
    end if
    allocate(A(507, 10, 5, 17), B(1, 1, 5, 17), R(507, 5, 17), Temp(507, 10, 5, 17))
    
    open(newunit=timesfile, file=trim(timesfilename), status='replace', iostat=ios)
    if (ios /= 0) then
        print *, "Error opening file: ", trim(timesfilename)
    endif
    open(newunit=resultsfile, file=trim(resultsfilename), access='stream', form='unformatted', status='replace', iostat=ios)
    if (ios /= 0) then
        print *, "Error opening file: ", trim(resultsfilename)
    endif
    ! init A,B
    AV = 0
    BV = 0
    do i4 = 1,size(A,4) 
        do i3 = 1,size(A,3)
            B(1,1,i3,i4) = 1507 + BV
            BV = BV + 1
            do i2 = 1, size(A,2)
                do i1 = 1, size(A,1)
                    A(i1, i2, i3, i4) = 1507 + AV * 200
                    AV = AV + 1
                end do
            end do
        end do
    end do
     
    M0 = 1142251548
    shift = 17
    call system_clock(count=wallT, count_rate=clockrate)
    
    DO
        
        call system_clock(count=startT)
        if ((startT - wallT) / real(clockrate) > 10) exit
        do j = 1, rep
    
            ! perform: m0 & (A << shift)) | (~m0 & B)
            do i4 = 1, size(A,4) 
                do i3 = 1,size(A,3)
                    do i2 = 1, size(A,2)
                        do i1 = 1, size(A,1)
                            Temp(i1, i2, i3, i4) = or(and(m0, lshift(A(i1, i2, i3, i4), shift)), and(not(m0), B(1, 1, i3, i4)))
                        end do
                    end do
                end do
            end do

            !perform: sum(..., 1)
            R = sum(Temp, DIM=2)

        end do
            
        call system_clock(count=endT)
        write(timesfile, *) (endT - wallT) / real(clockrate) * 1000, (endT - startT) / real(clockrate) / real(rep) * 1000
        write(resultsfile) R
#ifdef OPTIM
        print *, "FORTRAN opt:   ", (endT - wallT) / real(clockrate), (endT - startT) / real(clockrate), R(1,1,1) 
#else
        print *, "FORTRAN noopt: ", (endT - wallT) / real(clockrate), (endT - startT) / real(clockrate), R(1,1,1) 
#endif

    end do
    close(timesfile)
    close(resultsfile)

    end program Getting_Started_ACC_I_FORTRAN

