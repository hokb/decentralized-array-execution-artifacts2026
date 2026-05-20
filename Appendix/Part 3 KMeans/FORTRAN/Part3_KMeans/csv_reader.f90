module csv_reader
    implicit none
    private
    public :: read_csv

contains

    function read_csv(filename, nrows, ncols) result(data_array)
        character(len=*), intent(in) :: filename
        integer, intent(out) :: nrows, ncols
        real, allocatable :: data_array(:,:)
        
        integer :: unit, i, j, rc, file_stat
        character(len=1000) :: line
        character(len=30), allocatable :: temp_values(:)
        real :: temp_val
        logical :: file_exists
        
        ! Initialize defaults
        nrows = 0
        ncols = 0
        
        ! Check if file exists
        inquire(file=trim(filename), exist=file_exists)
        if (.not. file_exists) then
            write(*,*) "Error: File ", trim(filename), " does not exist"
            return
        end if
        
        ! First pass: count rows and columns
        open(newunit=unit, file=trim(filename), status='old', action='read')
        do 
            read(unit, '(a)', iostat=rc) line
            if (rc /= 0) exit
            
            ! Skip empty lines
            if (len_trim(line) == 0) cycle
            
            ! Count columns on first non-empty line
            if (nrows == 0) then
                ncols = count_commas(line) + 1
                allocate(temp_values(ncols))
            end if
            
            nrows = nrows + 1
        end do
        
        ! Allocate array
        allocate(data_array(nrows, ncols))
        
        ! Second pass: read data
        rewind(unit)
        do i = 1, nrows
            read(unit, '(a)') line
            call split_line(line, temp_values, ncols)
            
            do j = 1, ncols
                read(temp_values(j), *, iostat=rc) temp_val
                if (rc /= 0) temp_val = 0.0  ! Default for non-numeric
                data_array(i,j) = temp_val
            end do
        end do
        
        close(unit)
        
    contains
    
        function count_commas(str) result(n)
            character(len=*), intent(in) :: str
            integer :: n, i
            n = 0
            do i = 1, len_trim(str)
                if (str(i:i) == ',') n = n + 1
            end do
        end function count_commas
        
        subroutine split_line(line, values, n)
            character(len=*), intent(in) :: line
            character(len=*), intent(out) :: values(:)
            integer, intent(in) :: n
            
            integer :: i, start_pos, end_pos, current_col
            
            current_col = 1
            start_pos = 1
            
            do i = 1, len_trim(line)
                if (line(i:i) == ',' .or. i == len_trim(line)) then
                    if (i == len_trim(line)) then
                        end_pos = i
                    else
                        end_pos = i - 1
                    end if
                    
                    values(current_col) = adjustl(line(start_pos:end_pos))
                    current_col = current_col + 1
                    start_pos = i + 1
                    
                    if (current_col > n) exit
                end if
            end do
        end subroutine split_line

    end function read_csv

end module csv_reader