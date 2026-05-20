# Build stage
FROM mcr.microsoft.com/dotnet/aspnet:6.0  
WORKDIR /Artefact1
COPY FORTRAN ./FORTRAN/

COPY numpy ./numpy/
COPY ILNumerics/*.* ./ILNumerics/
COPY ILNumerics/publish ./ILNumerics/publish/
COPY ILNumerics/Properties ./ILNumerics/Properties/
COPY ILNumerics/ILNumerics_deploy ./ILNumerics/ILNumerics_deploy/

RUN apt-get update && \
    apt-get install -y --no-install-recommends gfortran python3 python3-pip python3-venv build-essential libgdiplus && \
    rm -rf /var/lib/apt/lists/*

RUN python3 -m venv /opt/venv
ENV PATH="/opt/venv/bin:$PATH"

RUN /opt/venv/bin/pip install --upgrade pip && \
    /opt/venv/bin/pip install numpy numba

WORKDIR FORTRAN
RUN gfortran -O3 Part1.f90 -o Part1_F90_optim -cpp -DOPTIM
RUN gfortran -O0 Part1.f90 -o Part1_F90_nooptim -cpp 

WORKDIR ../ILNumerics/publish/Release/net6.0/

ENTRYPOINT ["dotnet", "Part1.dll"]