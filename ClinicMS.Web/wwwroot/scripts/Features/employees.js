// Employees JS
var employees = [
    {id:1,name:'Ahmed Hassan',phone:'+20 100 123 4567',email:'ahmed.h@gym.com',gender:'Male',dob:'1990-05-15',nid:'29005150012345',type:'Full-time',branch:'Main Branch',address:'10 Tahrir Sq, Cairo',status:'Active'},
    {id:2,name:'Sara Mohamed',phone:'+20 101 234 5678',email:'sara.m@gym.com',gender:'Female',dob:'1995-03-22',nid:'29503220023456',type:'Full-time',branch:'Main Branch',address:'25 Nasr City, Cairo',status:'Active'},
    {id:3,name:'Khaled Ibrahim',phone:'+20 102 345 6789',email:'khaled.i@gym.com',gender:'Male',dob:'1988-11-08',nid:'28811080034567',type:'Trainer',branch:'North Branch',address:'5 Alexandria Road',status:'Active'},
    {id:4,name:'Fatima Nour',phone:'+20 103 456 7890',email:'fatima.n@gym.com',gender:'Female',dob:'1993-07-30',nid:'29307300045678',type:'Part-time',branch:'Main Branch',address:'12 Dokki, Giza',status:'Active'},
    {id:5,name:'Youssef Mohamed',phone:'+20 104 567 8901',email:'youssef.m@gym.com',gender:'Male',dob:'1985-02-14',nid:'28502140056789',type:'Full-time',branch:'South Branch',address:'33 Maadi, Cairo',status:'Active'},
    {id:6,name:'Nour Ali',phone:'+20 105 678 9012',email:'nour.a@gym.com',gender:'Female',dob:'1997-09-10',nid:'29709100067890',type:'Part-time',branch:'East Branch',address:'7 Heliopolis, Cairo',status:'Inactive'},
    {id:7,name:'Omar Khalil',phone:'+20 106 789 0123',email:'omar.k@gym.com',gender:'Male',dob:'1992-04-25',nid:'29204250078901',type:'Trainer',branch:'Main Branch',address:'15 Zamalek, Cairo',status:'Active'},
    {id:8,name:'Hana Sami',phone:'+20 107 890 1234',email:'hana.s@gym.com',gender:'Female',dob:'1994-12-05',nid:'29412050089012',type:'Trainer',branch:'North Branch',address:'9 Mohandeseen',status:'Active'},
    {id:9,name:'Tarek Fawzy',phone:'+20 108 901 2345',email:'tarek.f@gym.com',gender:'Male',dob:'1980-06-18',nid:'28006180090123',type:'Full-time',branch:'Main Branch',address:'20 Garden City',status:'Active'},
    {id:10,name:'Randa Hassan',phone:'+20 109 012 3456',email:'randa.h@gym.com',gender:'Female',dob:'1991-08-28',nid:'29108280001234',type:'Full-time',branch:'South Branch',address:'44 Rehab City',status:'Active'},
    {id:11,name:'Samy Ramadan',phone:'+20 110 123 4567',email:'samy.r@gym.com',gender:'Male',dob:'1996-01-12',nid:'29601120012345',type:'Intern',branch:'East Branch',address:'3 New Cairo',status:'Inactive'},
    {id:12,name:'Dina Wahba',phone:'+20 111 234 5678',email:'dina.w@gym.com',gender:'Female',dob:'1998-10-20',nid:'29810200023456',type:'Part-time',branch:'Main Branch',address:'18 Agouza',status:'Active'},
    {id:13,name:'Mohamed Saad',phone:'+20 112 345 6789',email:'mohamed.s@gym.com',gender:'Male',dob:'1987-03-05',nid:'28703050034567',type:'Contractor',branch:'North Branch',address:'6 Imbaba',status:'Active'},
    {id:14,name:'Laila Mostafa',phone:'+20 113 456 7890',email:'laila.m@gym.com',gender:'Female',dob:'1993-05-17',nid:'29305170045678',type:'Full-time',branch:'South Branch',address:'28 Shubra',status:'Active'},
    {id:15,name:'Hassan Ali',phone:'+20 114 567 8901',email:'hassan.a@gym.com',gender:'Male',dob:'1989-07-22',nid:'28907220056789',type:'Trainer',branch:'East Branch',address:'11 Ain Shams',status:'Active'}
];

var currentPage=1, perPage=8, searchQuery='', typeF='', statusF='', branchF='', editingId=null;
var avatarColors = ['#0d9488','#2563eb','#7c3aed','#dc2626','#d97706','#16a34a'];

function initials(name) { return name.split(' ').map(p=>p[0]).join('').toUpperCase().slice(0,2); }
function avatarColor(i) { return avatarColors[i%avatarColors.length]; }

function filtered() {
    var q = searchQuery.toLowerCase();
    return employees.filter(e => {
        var match = !q || e.name.toLowerCase().includes(q) || e.phone.includes(q) || e.email.toLowerCase().includes(q);
        return match && (!typeF||e.type===typeF) && (!statusF||e.status===statusF) && (!branchF||e.branch===branchF);
    });
}

function renderTable() {
    var data = filtered();
    var total = data.length;
    var pages = Math.ceil(total/perPage)||1;
    if (currentPage>pages) currentPage=1;
    var slice = data.slice((currentPage-1)*perPage, currentPage*perPage);
    document.getElementById('empTableBody').innerHTML = slice.length ? slice.map((e,i) => {
        var idx = employees.indexOf(e);
        return `<tr>
            <td>${(currentPage-1)*perPage+i+1}</td>
            <td><div style="display:flex;align-items:center;gap:10px;">
                <div class="gp-avatar" style="background:${avatarColor(idx)}">${initials(e.name)}</div>
                <div><div style="font-weight:700;color:#1e293b;">${e.name}</div><div style="font-size:.72rem;color:#94a3b8;">${e.type}</div></div>
            </div></td>
            <td>${e.phone}</td>
            <td style="font-size:.8rem;">${e.email}</td>
            <td><span class="gp-badge ${e.gender==='Male'?'gp-badge-blue':'gp-badge-purple'}">${e.gender}</span></td>
            <td><span class="gp-badge gp-badge-teal">${e.type}</span></td>
            <td>${e.branch}</td>
            <td><span class="gp-badge ${e.status==='Active'?'gp-badge-green':'gp-badge-red'}">${e.status}</span></td>
            <td><div style="display:flex;gap:6px;">
                <button class="gp-btn-icon gp-btn-view" onclick="viewEmployee(${e.id})"><i class="ri-eye-line"></i></button>
                <button class="gp-btn-icon gp-btn-edit" onclick="openModal(${e.id})"><i class="ri-pencil-line"></i></button>
                <button class="gp-btn-icon gp-btn-delete" onclick="deleteEmployee(${e.id})"><i class="ri-delete-bin-line"></i></button>
            </div></td>
        </tr>`;
    }).join('') : '<tr><td colspan="9" class="text-center py-4 text-muted">No employees found</td></tr>';
    document.getElementById('pageInfo').textContent = `Showing ${slice.length} of ${total}`;
    var btns=document.getElementById('pageBtns'); btns.innerHTML='';
    for(var p=1;p<=pages;p++){var btn=document.createElement('button');btn.className='gp-page-btn'+(p===currentPage?' active':'');btn.textContent=p;btn.onclick=(function(pp){return function(){currentPage=pp;renderTable();};})(p);btns.appendChild(btn);}
    // stats
    document.getElementById('statTotal').textContent = employees.length;
    document.getElementById('statActive').textContent = employees.filter(e=>e.status==='Active').length;
    document.getElementById('statMain').textContent = employees.filter(e=>e.branch==='Main Branch').length;
    document.getElementById('statTrainers').textContent = employees.filter(e=>e.type==='Trainer').length;
}

function openModal(id) {
    editingId=id||null;
    document.getElementById('modalTitle').textContent=id?'Edit Employee':'Add Employee';
    if(id){
        var e=employees.find(x=>x.id===id);
        document.getElementById('fName').value=e.name;
        document.getElementById('fPhone').value=e.phone;
        document.getElementById('fEmail').value=e.email;
        document.querySelector('input[name="gender"][value="'+e.gender+'"]').checked=true;
        document.getElementById('fDOB').value=e.dob;
        document.getElementById('fNID').value=e.nid;
        document.getElementById('fType').value=e.type;
        document.getElementById('fBranch').value=e.branch;
        document.getElementById('fAddress').value=e.address;
        document.getElementById('fStatus').checked=e.status==='Active';
    } else {
        ['fName','fPhone','fEmail','fNID','fAddress'].forEach(f=>document.getElementById(f).value='');
        document.getElementById('fDOB').value='';
        document.getElementById('fStatus').checked=true;
    }
    new bootstrap.Modal(document.getElementById('empModal')).show();
}

function previewPhoto(input) {
    if(input.files&&input.files[0]){
        var reader=new FileReader();
        reader.onload=function(e){ document.getElementById('photoPreview').src=e.target.result; document.getElementById('photoPreview').style.display='block'; document.getElementById('photoIcon').style.display='none'; };
        reader.readAsDataURL(input.files[0]);
    }
}

function saveEmployee() {
    var name=document.getElementById('fName').value.trim();
    if(!name){toastr.error('Name is required');return;}
    var data={
        id:editingId||Date.now(), name, phone:document.getElementById('fPhone').value,
        email:document.getElementById('fEmail').value,
        gender:document.querySelector('input[name="gender"]:checked').value,
        dob:document.getElementById('fDOB').value, nid:document.getElementById('fNID').value,
        type:document.getElementById('fType').value, branch:document.getElementById('fBranch').value,
        address:document.getElementById('fAddress').value,
        status:document.getElementById('fStatus').checked?'Active':'Inactive'
    };
    if(editingId){ var idx=employees.findIndex(x=>x.id===editingId); employees[idx]=data; toastr.success('Employee updated'); }
    else{ employees.push(data); toastr.success('Employee added'); }
    bootstrap.Modal.getInstance(document.getElementById('empModal')).hide();
    renderTable();
}

function viewEmployee(id) {
    var e=employees.find(x=>x.id===id);
    var idx=employees.indexOf(e);
    document.getElementById('vAvatar').textContent=initials(e.name);
    document.getElementById('vAvatar').style.background=avatarColor(idx);
    document.getElementById('vName').textContent=e.name;
    document.getElementById('vType').textContent=e.type+' · '+e.branch;
    document.getElementById('vDetails').innerHTML=`
        <div class="view-row"><i class="ri-phone-line"></i><div><div class="view-row-label">Phone</div><div class="view-row-val">${e.phone}</div></div></div>
        <div class="view-row"><i class="ri-mail-line"></i><div><div class="view-row-label">Email</div><div class="view-row-val">${e.email}</div></div></div>
        <div class="view-row"><i class="ri-id-card-line"></i><div><div class="view-row-label">National ID</div><div class="view-row-val">${e.nid}</div></div></div>
        <div class="view-row"><i class="ri-calendar-line"></i><div><div class="view-row-label">Date of Birth</div><div class="view-row-val">${e.dob}</div></div></div>
        <div class="view-row"><i class="ri-map-pin-line"></i><div><div class="view-row-label">Address</div><div class="view-row-val">${e.address}</div></div></div>`;
    new bootstrap.Modal(document.getElementById('viewEmpModal')).show();
}

function deleteEmployee(id) {
    if(!confirm('Delete this employee?'))return;
    employees=employees.filter(e=>e.id!==id);
    toastr.success('Employee deleted');
    renderTable();
}

function handleSearch(v){searchQuery=v;currentPage=1;renderTable();}
function handleFilter(){typeF=document.getElementById('typeFilter').value;statusF=document.getElementById('statusFilter').value;branchF=document.getElementById('branchFilter').value;currentPage=1;renderTable();}
document.addEventListener('DOMContentLoaded',renderTable);

// -- Select2 init --
function initSelect2() {
    if (typeof $ !== 'undefined' && $.fn && $.fn.select2) {
        $('select.form-select, select.gp-select').select2({
            theme: 'default',
            width: '100%',
            dropdownParent: document.body
        });
    }
}
// Re-run on each modal open
document.addEventListener('show.bs.modal', function() { setTimeout(initSelect2, 50); });
document.addEventListener('DOMContentLoaded', initSelect2);
